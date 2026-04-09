using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using AppValidationEx = attendance_monitoring.Exceptions.ValidationException;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Models.DTO.Response.AdminData;
using attendance_monitoring.Options;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace attendance_monitoring.Services.AdminData;

public sealed class AdminDataService : IAdminDataService
{
    private static readonly string[] SupportedFormats = ["csv", "xlsx"];

    private const string RowStatusDuplicate = "duplicate";
    private const string RowStatusFailed = "failed";
    private const string RowStatusImported = "imported";
    private const string RowStatusInvalid = "invalid";
    private const string RowStatusReady = "ready";

    private const string RollbackIssueCode = "import_rollback";
    private const string RollbackIssueMessage = "Row was imported but rolled back because another row failed.";

    private readonly IAccountService _accountService;
    private readonly IClassroomService _classroomService;
    private readonly ApplicationDbContext _context;
    private readonly ICourseService _courseService;
    private readonly ILogger<AdminDataService> _logger;
    private readonly BulkDataOptions _options;
    private readonly ISectionService _sectionService;
    private readonly IScheduleService _scheduleService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly ISubjectService _subjectService;

    public AdminDataService(
        ApplicationDbContext context,
        IAccountService accountService,
        ICourseService courseService,
        IClassroomService classroomService,
        ISectionService sectionService,
        IScheduleService scheduleService,
        IStudentEnrollmentService studentEnrollmentService,
        ISubjectService subjectService,
        IOptions<BulkDataOptions> options,
        ILogger<AdminDataService> logger)
    {
        _context = context;
        _accountService = accountService;
        _courseService = courseService;
        _classroomService = classroomService;
        _sectionService = sectionService;
        _scheduleService = scheduleService;
        _studentEnrollmentService = studentEnrollmentService;
        _subjectService = subjectService;
        _options = options.Value;
        _logger = logger;
    }

    public Task<AdminDataPreviewResponseDto> PreviewImportAsync(string entity, IFormFile file, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default)
        => BuildPreviewResponseAsync(entity, file, filters, cancellationToken);

    public async Task<AdminDataImportResponseDto> ImportAsync(string entity, IFormFile file, ClaimsPrincipal user, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default)
    {
        var analysis = await AnalyzeImportCoreAsync(entity, file, filters, cancellationToken).ConfigureAwait(false);
        var response = new AdminDataImportResponseDto
        {
            Success = false,
            Entity = analysis.Entity,
            Format = analysis.Format,
            FileName = analysis.FileName,
            TotalRows = analysis.TotalRows,
            FileIssues = analysis.FileIssues.Select(CloneIssue).ToList(),
            Rows = analysis.Rows.Select(CloneRow).ToList(),
        };

        if (!analysis.CanImport)
        {
            response.FailedRows = analysis.InvalidRows;
            response.SkippedDuplicateRows = analysis.DuplicateRows;
            ApplyIssueLimit(response.FileIssues, response.Rows);
            return response;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (var row in response.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(row.Status, RowStatusDuplicate, StringComparison.OrdinalIgnoreCase))
                {
                    response.SkippedDuplicateRows++;
                    continue;
                }

                if (!string.Equals(row.Status, RowStatusReady, StringComparison.OrdinalIgnoreCase))
                {
                    response.FailedRows++;
                    continue;
                }

                try
                {
                    await ImportRowAsync(analysis.Entity, row.Values, user, analysis.Lookup, cancellationToken).ConfigureAwait(false);
                    row.Status = RowStatusImported;
                    response.CreatedRows++;
                }
                catch (Exception ex) when (ex is EntityAlreadyExistsException<string> or EntityAlreadyExistsException<int>)
                {
                    row.Status = RowStatusDuplicate;
                    row.Issues.Add(CreateIssue(row.RowNumber, "duplicate", "warning", ex.Message));
                    response.SkippedDuplicateRows++;
                }
                catch (Exception ex) when (ex is AppValidationEx or EntityNotFoundException<int> or EntityNotFoundException<string> or EntityServiceException)
                {
                    row.Status = RowStatusFailed;
                    row.Issues.Add(CreateIssue(row.RowNumber, "import_failed", "error", ex.Message));
                    response.FailedRows++;
                }
            }

            if (response.FailedRows > 0)
            {
                await RollbackWithoutCancellationAsync(transaction).ConfigureAwait(false);

                foreach (var row in response.Rows)
                {
                    if (string.Equals(row.Status, RowStatusImported, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Status = RowStatusFailed;
                        row.Issues.Add(CreateIssue(row.RowNumber, RollbackIssueCode, "error", RollbackIssueMessage));
                        response.CreatedRows--;
                        response.FailedRows++;
                    }
                }

                response.Success = false;
                ApplyIssueLimit(response.FileIssues, response.Rows);
                return response;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            response.Success = response.FailedRows == 0;
        }
        catch
        {
            await RollbackWithoutCancellationAsync(transaction).ConfigureAwait(false);

            foreach (var row in response.Rows)
            {
                if (string.Equals(row.Status, RowStatusImported, StringComparison.OrdinalIgnoreCase))
                {
                    row.Status = RowStatusFailed;
                    row.Issues.Add(CreateIssue(row.RowNumber, RollbackIssueCode, "error", RollbackIssueMessage));
                    response.CreatedRows--;
                    response.FailedRows++;
                }
            }

            response.Success = false;
            throw;
        }

        ApplyIssueLimit(response.FileIssues, response.Rows);
        return response;
    }

    private static Task RollbackWithoutCancellationAsync(IDbContextTransaction transaction)
        => transaction.RollbackAsync(CancellationToken.None);

    public async Task<AdminDataFileDto> GenerateTemplateAsync(string entity, string format, CancellationToken cancellationToken = default)
    {
        var normalizedEntity = NormalizeEntity(entity);
        var normalizedFormat = NormalizeFormat(format);
        var columns = GetColumns(normalizedEntity);
        var fileName = $"{normalizedEntity}-template.{normalizedFormat}";

        return normalizedFormat switch
        {
            "csv" => new AdminDataFileDto
            {
                Content = await BuildCsvAsync(columns, Array.Empty<IReadOnlyDictionary<string, string?>>(), cancellationToken).ConfigureAwait(false),
                ContentType = "text/csv",
                FileName = fileName,
            },
            "xlsx" => new AdminDataFileDto
            {
                Content = BuildWorkbook(columns, Array.Empty<IReadOnlyDictionary<string, string?>>(), normalizedEntity, includeInstructions: true),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName,
            },
            _ => throw new AppValidationEx($"Unsupported format '{format}'."),
        };
    }

    public async Task<AdminDataFileDto> ExportAsync(string entity, string format, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken = default)
    {
        var normalizedEntity = NormalizeEntity(entity);
        var normalizedFormat = NormalizeFormat(format);
        var columns = GetColumns(normalizedEntity);
        var rows = await GetExportRowsAsync(normalizedEntity, filters, cancellationToken).ConfigureAwait(false);
        var fileName = $"{normalizedEntity}-export-{DateTime.UtcNow:yyyyMMddHHmmss}.{normalizedFormat}";

        return normalizedFormat switch
        {
            "csv" => new AdminDataFileDto
            {
                Content = await BuildCsvAsync(columns, rows, cancellationToken).ConfigureAwait(false),
                ContentType = "text/csv",
                FileName = fileName,
            },
            "xlsx" => new AdminDataFileDto
            {
                Content = BuildWorkbook(columns, rows, normalizedEntity, includeInstructions: false),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName,
            },
            _ => throw new AppValidationEx($"Unsupported format '{format}'."),
        };
    }

    private async Task<AdminDataPreviewResponseDto> BuildPreviewResponseAsync(string entity, IFormFile file, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken)
    {
        var analysis = await AnalyzeImportCoreAsync(entity, file, filters, cancellationToken).ConfigureAwait(false);
        var response = new AdminDataPreviewResponseDto
        {
            Success = true,
            Entity = analysis.Entity,
            Format = analysis.Format,
            FileName = analysis.FileName,
            TotalRows = analysis.TotalRows,
            Columns = analysis.Columns.ToList(),
            Rows = analysis.Rows.Take(_options.MaxPreviewRows).Select(CloneRow).ToList(),
            ReadyRows = analysis.ReadyRows,
            DuplicateRows = analysis.DuplicateRows,
            InvalidRows = analysis.InvalidRows,
            CanImport = analysis.CanImport,
            FileIssues = analysis.FileIssues.Select(CloneIssue).ToList(),
        };

        ApplyIssueLimit(response.FileIssues, response.Rows);
        return response;
    }

    private async Task<ImportAnalysisResult> AnalyzeImportCoreAsync(string entity, IFormFile file, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken)
    {
        var normalizedEntity = NormalizeEntity(entity);
        EnsureValidFile(file);
        var format = DetectFormat(file.FileName);
        var parsedRows = await ParseRowsAsync(file, format, cancellationToken).ConfigureAwait(false);

        if (parsedRows.Count > _options.MaxRows)
        {
            throw new AppValidationEx($"File contains {parsedRows.Count} rows which exceeds the limit of {_options.MaxRows}.");
        }

        var lookup = await CreateLookupAsync(cancellationToken).ConfigureAwait(false);
        var rowResults = await AnalyzeRowsAsync(normalizedEntity, parsedRows, filters, lookup, cancellationToken).ConfigureAwait(false);
        return new ImportAnalysisResult(
            normalizedEntity,
            format,
            file.FileName,
            parsedRows.Count,
            GetColumns(normalizedEntity).ToList(),
            [],
            rowResults,
            rowResults.Count(row => string.Equals(row.Status, RowStatusReady, StringComparison.OrdinalIgnoreCase)),
            rowResults.Count(row => string.Equals(row.Status, RowStatusDuplicate, StringComparison.OrdinalIgnoreCase)),
            rowResults.Count(row => string.Equals(row.Status, RowStatusInvalid, StringComparison.OrdinalIgnoreCase)),
            rowResults.All(row => !string.Equals(row.Status, RowStatusInvalid, StringComparison.OrdinalIgnoreCase)),
            lookup);
    }

    private async Task<List<AdminDataRowResultDto>> AnalyzeRowsAsync(string entity, IReadOnlyList<Dictionary<string, string?>> parsedRows, IReadOnlyDictionary<string, string?> filters, LookupCache lookup, CancellationToken cancellationToken)
    {
        var results = new List<AdminDataRowResultDto>(parsedRows.Count);
        var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < parsedRows.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowNumber = index + 2;
            var normalizedValues = NormalizeRowValues(entity, parsedRows[index]);
            ApplyScopedImportValues(entity, normalizedValues, filters, rowNumber);
            var row = new AdminDataRowResultDto
            {
                RowNumber = rowNumber,
                Status = RowStatusReady,
                Values = normalizedValues,
            };

            ValidateRequiredColumns(entity, row);
            ValidateRow(entity, row, lookup);

            var duplicateKey = BuildDuplicateKey(entity, row.Values, lookup);
            if (!string.IsNullOrWhiteSpace(duplicateKey))
            {
                if (!duplicateKeys.Add(duplicateKey))
                {
                    row.Status = RowStatusDuplicate;
                    row.Issues.Add(CreateIssue(row.RowNumber, "duplicate_in_file", "warning", "Duplicate row in file will be skipped."));
                }
                else if (ExistsInDatabase(entity, row.Values, lookup))
                {
                    row.Status = RowStatusDuplicate;
                    row.Issues.Add(CreateIssue(row.RowNumber, "duplicate_existing", "warning", "Matching record already exists and will be skipped."));
                }
            }

            if (row.Issues.Any(issue => string.Equals(issue.Severity, "error", StringComparison.OrdinalIgnoreCase)))
            {
                row.Status = RowStatusInvalid;
            }

            results.Add(row);
        }

        return results;
    }

    private static void ApplyScopedImportValues(string entity, Dictionary<string, string?> values, IReadOnlyDictionary<string, string?> filters, int rowNumber)
    {
        if (!string.Equals(entity, "enrollments", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!filters.TryGetValue("sectionName", out var scopedSectionName) || string.IsNullOrWhiteSpace(scopedSectionName))
        {
            return;
        }

        var currentSectionName = values.GetValueOrDefault("sectionname");
        if (string.IsNullOrWhiteSpace(currentSectionName))
        {
            values["sectionname"] = scopedSectionName;
            return;
        }

        if (!string.Equals(currentSectionName, scopedSectionName, StringComparison.OrdinalIgnoreCase))
        {
            values["sectionname"] = $"__scope_mismatch__:{currentSectionName}";
        }
    }

    private static void ValidateRequiredColumns(string entity, AdminDataRowResultDto row)
    {
        foreach (var column in GetColumns(entity))
        {
            var normalizedColumn = NormalizeColumnName(column);
            if (!row.Values.ContainsKey(normalizedColumn))
            {
                row.Issues.Add(CreateIssue(row.RowNumber, "missing_column", "error", $"Column '{column}' is required.", column));
                continue;
            }

            if (IsRequiredColumn(entity, column) && string.IsNullOrWhiteSpace(row.Values[normalizedColumn]))
            {
                row.Issues.Add(CreateIssue(row.RowNumber, "required", "error", $"Column '{column}' is required.", column));
            }
        }
    }

    private static void ValidateRow(string entity, AdminDataRowResultDto row, LookupCache lookup)
    {
        switch (entity)
        {
            case "users":
                ValidateUserRow(row, lookup);
                break;
            case "courses":
                ValidateDataAnnotations(new CreateCourse
                {
                    Name = row.Values.GetValueOrDefault("name") ?? string.Empty,
                }, row);
                break;
            case "sections":
                ValidateSectionRow(row, lookup);
                break;
            case "subjects":
                ValidateDataAnnotations(new CreateSubject
                {
                    Name = row.Values.GetValueOrDefault("name") ?? string.Empty,
                    Code = row.Values.GetValueOrDefault("code") ?? string.Empty,
                }, row);
                break;
            case "classrooms":
                ValidateDataAnnotations(new CreateClassroom
                {
                    Name = row.Values.GetValueOrDefault("name") ?? string.Empty,
                }, row);
                break;
            case "schedules":
                ValidateScheduleRow(row, lookup);
                break;
            case "enrollments":
                ValidateEnrollmentRow(row, lookup);
                break;
            default:
                throw new AppValidationEx($"Unsupported entity '{entity}'.");
        }
    }

    private static void ValidateUserRow(AdminDataRowResultDto row, LookupCache lookup)
    {
        var role = NormalizeRole(row.Values.GetValueOrDefault("role"));
        row.Values["role"] = role;
        var sectionName = row.Values.GetValueOrDefault("sectionname");

        var dto = new RegisterDto
        {
            Username = row.Values.GetValueOrDefault("username") ?? string.Empty,
            Email = row.Values.GetValueOrDefault("email") ?? string.Empty,
            Firstname = row.Values.GetValueOrDefault("firstname"),
            Lastname = row.Values.GetValueOrDefault("lastname"),
            Password = row.Values.GetValueOrDefault("temporarypassword") ?? string.Empty,
            RepeatedPassword = row.Values.GetValueOrDefault("temporarypassword") ?? string.Empty,
            Role = role,
            SectionId = role == RoleConstants.Student && !string.IsNullOrWhiteSpace(sectionName) && lookup.SectionIdsByName.TryGetValue(sectionName, out var sectionId)
                ? sectionId
                : null,
        };

        if (role == RoleConstants.Student && string.IsNullOrWhiteSpace(sectionName))
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "missing_reference", "error", "Students require sectionName.", "sectionName"));
        }
        else if (role == RoleConstants.Student && !lookup.SectionIdsByName.ContainsKey(sectionName!))
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "missing_reference", "error", $"Section '{sectionName}' was not found.", "sectionName"));
        }

        ValidateDataAnnotations(dto, row);
    }

    private static void ValidateSectionRow(AdminDataRowResultDto row, LookupCache lookup)
    {
        var courseName = row.Values.GetValueOrDefault("coursename");
        var courseId = 0;
        if (string.IsNullOrWhiteSpace(courseName) || !lookup.CourseIdsByName.TryGetValue(courseName, out courseId))
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "missing_reference", "error", $"Course '{courseName}' was not found.", "courseName"));
        }

        ValidateDataAnnotations(new CreateSection
        {
            Name = row.Values.GetValueOrDefault("name") ?? string.Empty,
            CourseId = courseId,
        }, row);
    }

    private static void ValidateScheduleRow(AdminDataRowResultDto row, LookupCache lookup)
    {
        var createSchedule = new CreateSchedule
        {
            DayOfWeek = row.Values.GetValueOrDefault("dayofweek") ?? string.Empty,
            TimeIn = ParseTime(row, "timein"),
            TimeOut = ParseTime(row, "timeout"),
            SubjectId = ResolveLookup(lookup.SubjectIdsByCode, row, "subjectcode", "Subject"),
            ClassroomId = ResolveLookup(lookup.ClassroomIdsByName, row, "classroomname", "Classroom"),
            SectionId = ResolveLookup(lookup.SectionIdsByName, row, "sectionname", "Section"),
            InstructorId = ResolveLookup(lookup.InstructorIdsByEmail, row, "instructoremail", "Instructor"),
        };

        ValidateDataAnnotations(createSchedule, row);

        if (!string.IsNullOrWhiteSpace(createSchedule.DayOfWeek) && !ScheduleConstants.IsValidDayOfWeek(createSchedule.DayOfWeek))
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "invalid_day", "error", $"Invalid DayOfWeek '{createSchedule.DayOfWeek}'.", "dayOfWeek"));
        }

        var timeInFailed = row.Issues.Any(issue =>
            string.Equals(issue.Code, "invalid_time", StringComparison.OrdinalIgnoreCase)
            && string.Equals(issue.Field, "timein", StringComparison.OrdinalIgnoreCase));
        var timeOutFailed = row.Issues.Any(issue =>
            string.Equals(issue.Code, "invalid_time", StringComparison.OrdinalIgnoreCase)
            && string.Equals(issue.Field, "timeout", StringComparison.OrdinalIgnoreCase));

        if (!timeInFailed && !timeOutFailed && createSchedule.TimeOut <= createSchedule.TimeIn)
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "invalid_time_range", "error", "timeOut must be after timeIn.", "timeOut"));
        }
    }

    private static void ValidateEnrollmentRow(AdminDataRowResultDto row, LookupCache lookup)
    {
        var sectionName = row.Values.GetValueOrDefault("sectionname");
        if (!string.IsNullOrWhiteSpace(sectionName) && sectionName.StartsWith("__scope_mismatch__:", StringComparison.Ordinal))
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "scope_mismatch", "error", "Row sectionName does not match the section currently being managed.", "sectionName"));
            sectionName = sectionName.Replace("__scope_mismatch__:", string.Empty, StringComparison.Ordinal);
            row.Values["sectionname"] = sectionName;
        }

        var studentId = ResolveLookup(lookup.StudentIdsByEmail, row, "studentemail", "Student");
        var sectionId = ResolveLookup(lookup.SectionIdsByName, row, "sectionname", "Section");
        var subjectId = ResolveLookup(lookup.SubjectIdsByCode, row, "subjectcode", "Subject");

        if (studentId != 0 && sectionId != 0 && lookup.StudentPrimarySectionIds.TryGetValue(studentId, out var primarySectionId) && primarySectionId == sectionId)
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "duplicate_existing", "warning", "Student already belongs to this section as their primary section and will be skipped."));
        }

        ValidateDataAnnotations(new CreateStudentEnrollment
        {
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            EnrollmentType = row.Values.GetValueOrDefault("enrollmenttype") ?? "Regular",
            AcademicYear = NullIfEmpty(row.Values.GetValueOrDefault("academicyear")),
            Semester = NullIfEmpty(row.Values.GetValueOrDefault("semester")),
        }, row);
    }

    private static void ValidateDataAnnotations(object instance, AdminDataRowResultDto row)
    {
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(instance);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, context, validationResults, true))
        {
            return;
        }

        foreach (var validationResult in validationResults)
        {
            row.Issues.Add(CreateIssue(
                row.RowNumber,
                "validation",
                "error",
                validationResult.ErrorMessage ?? "Validation failed.",
                validationResult.MemberNames.FirstOrDefault()));
        }
    }

    private async Task ImportRowAsync(string entity, IReadOnlyDictionary<string, string?> values, ClaimsPrincipal user, LookupCache lookup, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (entity)
        {
            case "users":
                await ImportUserAsync(values, lookup).ConfigureAwait(false);
                break;
            case "courses":
                await _courseService.CreateCourseAsync(new CreateCourse
                {
                    Name = values.GetValueOrDefault("name") ?? string.Empty,
                }, user).ConfigureAwait(false);
                break;
            case "sections":
                await ImportSectionAsync(values, lookup).ConfigureAwait(false);
                break;
            case "subjects":
                await _subjectService.CreateSubjectAsync(new CreateSubject
                {
                    Name = values.GetValueOrDefault("name") ?? string.Empty,
                    Code = values.GetValueOrDefault("code") ?? string.Empty,
                }).ConfigureAwait(false);
                break;
            case "classrooms":
                await _classroomService.CreateClassroomAsync(new CreateClassroom
                {
                    Name = values.GetValueOrDefault("name") ?? string.Empty,
                }).ConfigureAwait(false);
                break;
            case "schedules":
                await ImportScheduleAsync(values, lookup).ConfigureAwait(false);
                break;
            case "enrollments":
                await ImportEnrollmentAsync(values, lookup).ConfigureAwait(false);
                break;
            default:
                throw new AppValidationEx($"Unsupported entity '{entity}'.");
        }
    }

    private async Task ImportUserAsync(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var role = NormalizeRole(values.GetValueOrDefault("role"));
        var sectionName = values.GetValueOrDefault("sectionname");
        int? sectionId = null;

        if (role == RoleConstants.Student && !string.IsNullOrWhiteSpace(sectionName))
        {
            if (lookup.SectionIdsByName.TryGetValue(sectionName, out var cachedSectionId))
            {
                sectionId = cachedSectionId;
            }
            else
            {
                throw new EntityNotFoundException<string>("Section", sectionName, $"Section '{sectionName}' was not found.");
            }
        }

        await _accountService.RegisterAsync(new RegisterDto
        {
            Username = values.GetValueOrDefault("username") ?? string.Empty,
            Email = values.GetValueOrDefault("email") ?? string.Empty,
            Firstname = values.GetValueOrDefault("firstname"),
            Lastname = values.GetValueOrDefault("lastname"),
            Password = values.GetValueOrDefault("temporarypassword") ?? string.Empty,
            RepeatedPassword = values.GetValueOrDefault("temporarypassword") ?? string.Empty,
            Role = role,
            SectionId = sectionId,
        }).ConfigureAwait(false);
    }

    private async Task ImportSectionAsync(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var courseName = values.GetValueOrDefault("coursename") ?? string.Empty;
        if (!lookup.CourseIdsByName.TryGetValue(courseName, out var courseId))
        {
            throw new EntityNotFoundException<string>("Course", courseName, $"Course '{courseName}' was not found.");
        }

        await _sectionService.CreateSectionAsync(new Section
        {
            Name = values.GetValueOrDefault("name") ?? string.Empty,
            CourseId = courseId,
        }).ConfigureAwait(false);
    }

    private async Task ImportScheduleAsync(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var subjectCode = values.GetValueOrDefault("subjectcode") ?? string.Empty;
        var classroomName = values.GetValueOrDefault("classroomname") ?? string.Empty;
        var sectionName = values.GetValueOrDefault("sectionname") ?? string.Empty;
        var instructorEmail = values.GetValueOrDefault("instructoremail") ?? string.Empty;

        if (!lookup.SubjectIdsByCode.TryGetValue(subjectCode, out var subjectId))
        {
            throw new EntityNotFoundException<string>("Subject", subjectCode, $"Subject '{subjectCode}' was not found.");
        }
        if (!lookup.ClassroomIdsByName.TryGetValue(classroomName, out var classroomId))
        {
            throw new EntityNotFoundException<string>("Classroom", classroomName, $"Classroom '{classroomName}' was not found.");
        }
        if (!lookup.SectionIdsByName.TryGetValue(sectionName, out var sectionId))
        {
            throw new EntityNotFoundException<string>("Section", sectionName, $"Section '{sectionName}' was not found.");
        }
        if (!lookup.InstructorIdsByEmail.TryGetValue(instructorEmail, out var instructorId))
        {
            throw new EntityNotFoundException<string>("Instructor", instructorEmail, $"Instructor '{instructorEmail}' was not found.");
        }

        await _scheduleService.CreateScheduleAsync(new CreateSchedule
        {
            DayOfWeek = values.GetValueOrDefault("dayofweek") ?? string.Empty,
            TimeIn = ParseTime(values.GetValueOrDefault("timein")),
            TimeOut = ParseTime(values.GetValueOrDefault("timeout")),
            SubjectId = subjectId,
            ClassroomId = classroomId,
            SectionId = sectionId,
            InstructorId = instructorId,
        }).ConfigureAwait(false);
    }

    private async Task ImportEnrollmentAsync(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var studentEmail = values.GetValueOrDefault("studentemail") ?? string.Empty;
        var sectionName = values.GetValueOrDefault("sectionname") ?? string.Empty;
        var subjectCode = values.GetValueOrDefault("subjectcode") ?? string.Empty;

        if (!lookup.StudentIdsByEmail.TryGetValue(studentEmail, out var studentId))
        {
            throw new EntityNotFoundException<string>("Student", studentEmail, $"Student '{studentEmail}' was not found.");
        }
        if (!lookup.SectionIdsByName.TryGetValue(sectionName, out var sectionId))
        {
            throw new EntityNotFoundException<string>("Section", sectionName, $"Section '{sectionName}' was not found.");
        }
        if (!lookup.SubjectIdsByCode.TryGetValue(subjectCode, out var subjectId))
        {
            throw new EntityNotFoundException<string>("Subject", subjectCode, $"Subject '{subjectCode}' was not found.");
        }

        await _studentEnrollmentService.EnrollStudentAsync(
            studentId,
            sectionId,
            subjectId,
            values.GetValueOrDefault("enrollmenttype") ?? "Regular",
            NullIfEmpty(values.GetValueOrDefault("academicyear")),
            NullIfEmpty(values.GetValueOrDefault("semester"))).ConfigureAwait(false);
    }

    private async Task<List<IReadOnlyDictionary<string, string?>>> GetExportRowsAsync(string entity, IReadOnlyDictionary<string, string?> filters, CancellationToken cancellationToken)
    {
        return entity switch
        {
            "users" => await ExportUsersAsync(filters).ConfigureAwait(false),
            "courses" => (await _courseService.GetAllCoursesAsync().ConfigureAwait(false))
                .OrderBy(course => course.Name)
                .Select(course => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["name"] = course.Name,
                }).ToList(),
            "sections" => (await _context.Sections.AsNoTracking().Include(section => section.Course).OrderBy(section => section.Name).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Select(section => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["name"] = section.Name,
                    ["courseName"] = section.Course?.Name,
                }).ToList(),
            "subjects" => (await _subjectService.GetAllSubjectsAsync().ConfigureAwait(false))
                .OrderBy(subject => subject.Name)
                .Select(subject => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["code"] = subject.Code,
                    ["name"] = subject.Name,
                }).ToList(),
            "classrooms" => (await _classroomService.GetAllClassroomsAsync().ConfigureAwait(false))
                .OrderBy(classroom => classroom.Name)
                .Select(classroom => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["name"] = classroom.Name,
                }).ToList(),
            "schedules" => (await _scheduleService.GetAllSchedulesAsync().ConfigureAwait(false))
                .Select(schedule => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["dayOfWeek"] = schedule.DayOfWeek,
                    ["timeIn"] = schedule.TimeIn.ToString("HH:mm", CultureInfo.InvariantCulture),
                    ["timeOut"] = schedule.TimeOut.ToString("HH:mm", CultureInfo.InvariantCulture),
                    ["subjectCode"] = schedule.Subject?.Code,
                    ["sectionName"] = schedule.Section?.Name,
                    ["classroomName"] = schedule.Classroom?.Name,
                    ["instructorEmail"] = schedule.Instructor?.Email,
                }).ToList(),
            "enrollments" => (await BuildEnrollmentExportQuery(filters)
                    .OrderBy(enrollment => enrollment.Student.User.Email)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(enrollment => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
                {
                    ["studentEmail"] = enrollment.Student?.User?.Email,
                    ["sectionName"] = enrollment.Section?.Name,
                    ["subjectCode"] = enrollment.Subject?.Code,
                    ["enrollmentType"] = enrollment.EnrollmentType,
                    ["academicYear"] = enrollment.AcademicYear,
                    ["semester"] = enrollment.Semester,
                }).ToList(),
            _ => throw new AppValidationEx($"Unsupported entity '{entity}'."),
        };
    }

    private IQueryable<StudentEnrollment> BuildEnrollmentExportQuery(IReadOnlyDictionary<string, string?> filters)
    {
        var query = _context.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
                .ThenInclude(student => student.User)
            .Include(enrollment => enrollment.Section)
            .Include(enrollment => enrollment.Subject)
            .AsQueryable();

        if (filters.TryGetValue("sectionName", out var sectionName) && !string.IsNullOrWhiteSpace(sectionName))
        {
            query = query.Where(enrollment => enrollment.Section.Name == sectionName);
        }

        if (filters.TryGetValue("sectionId", out var sectionIdValue) && int.TryParse(sectionIdValue, out var sectionId))
        {
            query = query.Where(enrollment => enrollment.SectionId == sectionId);
        }

        return query;
    }

    private async Task<List<IReadOnlyDictionary<string, string?>>> ExportUsersAsync(IReadOnlyDictionary<string, string?> filters)
    {
        var statusValue = filters.TryGetValue("status", out var value) ? value : null;
        var status = Enum.TryParse<UserStatus>(statusValue, true, out var parsedStatus) ? parsedStatus : UserStatus.Active;
        var role = filters.TryGetValue("role", out var roleValue) ? roleValue : null;
        var search = filters.TryGetValue("search", out var searchValue) ? searchValue : null;
        var users = (await _accountService.GetAllUsersAsync(status).ConfigureAwait(false))
            .Where(user => MatchesUserExportFilters(user, role, search))
            .ToList();

        return users.Select(user =>
        {
            var firstName = user.StudentProfile?.Firstname ?? user.InstructorProfile?.Firstname ?? user.AdminProfile?.Firstname;
            var lastName = user.StudentProfile?.Lastname ?? user.InstructorProfile?.Lastname ?? user.AdminProfile?.Lastname;
            return (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>
            {
                ["username"] = user.Username,
                ["email"] = user.Email,
                ["firstname"] = firstName,
                ["lastname"] = lastName,
                ["role"] = user.Role,
                ["sectionName"] = user.StudentProfile?.SectionName,
                ["temporaryPassword"] = null,
            };
        }).ToList();
    }

    private async Task<LookupCache> CreateLookupAsync(CancellationToken cancellationToken)
    {
        var courses = await _context.Courses.AsNoTracking().ToDictionaryAsync(course => course.Name, course => course.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var sections = await _context.Sections.AsNoTracking().ToDictionaryAsync(section => section.Name, section => section.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var subjects = await _context.Subjects.AsNoTracking().ToDictionaryAsync(subject => subject.Code, subject => subject.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var classrooms = await _context.Classrooms.AsNoTracking().ToDictionaryAsync(classroom => classroom.Name, classroom => classroom.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var instructors = await _context.Instructors.AsNoTracking().Include(instructor => instructor.User).Where(instructor => !instructor.IsDeleted && instructor.User.Email != null).ToDictionaryAsync(instructor => instructor.User.Email!, instructor => instructor.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var students = await _context.Students.AsNoTracking().Include(student => student.User).Where(student => !student.IsDeleted && student.User.Email != null).ToDictionaryAsync(student => student.User.Email!, student => student.Id, StringComparer.OrdinalIgnoreCase, cancellationToken).ConfigureAwait(false);
        var primarySections = await _context.Students.AsNoTracking().Where(student => !student.IsDeleted).ToDictionaryAsync(student => student.Id, student => student.SectionId, cancellationToken).ConfigureAwait(false);
        var users = (await _accountService.GetAllUsersAsync(UserStatus.All).ConfigureAwait(false)).ToList();

        return new LookupCache(
            courses,
            sections,
            subjects,
            classrooms,
            instructors,
            students,
            primarySections,
            new HashSet<string>(users.Select(user => user.Username), StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(users.Select(user => user.Email), StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(courses.Keys, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(sections.Keys, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>((await _context.Subjects.AsNoTracking().Select(subject => $"{subject.Code}|{subject.Name}").ToListAsync(cancellationToken).ConfigureAwait(false)), StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(classrooms.Keys, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>((await _context.Schedules.AsNoTracking().Select(schedule => BuildScheduleKey(schedule.DayOfWeek, schedule.TimeIn, schedule.TimeOut, schedule.SubjectId, schedule.SectionId, schedule.ClassroomId, schedule.InstructorId)).ToListAsync(cancellationToken).ConfigureAwait(false)), StringComparer.OrdinalIgnoreCase),
            new HashSet<string>((await _context.StudentEnrollments.AsNoTracking().Select(enrollment => BuildEnrollmentKey(enrollment.StudentId, enrollment.SectionId, enrollment.SubjectId)).ToListAsync(cancellationToken).ConfigureAwait(false)), StringComparer.OrdinalIgnoreCase));
    }

    private static bool ExistsInDatabase(string entity, IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        return entity switch
        {
            "users" => lookup.ExistingUsernames.Contains(values.GetValueOrDefault("username") ?? string.Empty)
                || lookup.ExistingEmails.Contains(values.GetValueOrDefault("email") ?? string.Empty),
            "courses" => lookup.ExistingCourseNames.Contains(values.GetValueOrDefault("name") ?? string.Empty),
            "sections" => lookup.ExistingSectionNames.Contains(values.GetValueOrDefault("name") ?? string.Empty),
            "subjects" => lookup.ExistingSubjectKeys.Contains($"{values.GetValueOrDefault("code")}|{values.GetValueOrDefault("name")}"),
            "classrooms" => lookup.ExistingClassroomNames.Contains(values.GetValueOrDefault("name") ?? string.Empty),
            "schedules" => lookup.ExistingScheduleKeys.Contains(BuildScheduleKey(values, lookup)),
            "enrollments" => lookup.ExistingEnrollmentKeys.Contains(BuildEnrollmentKey(values, lookup)),
            _ => false,
        };
    }

    private static IReadOnlyList<string> GetColumns(string entity)
    {
        return entity switch
        {
            "users" => ["username", "email", "firstname", "lastname", "role", "sectionName", "temporaryPassword"],
            "courses" => ["name"],
            "sections" => ["name", "courseName"],
            "subjects" => ["code", "name"],
            "classrooms" => ["name"],
            "schedules" => ["dayOfWeek", "timeIn", "timeOut", "subjectCode", "sectionName", "classroomName", "instructorEmail"],
            "enrollments" => ["studentEmail", "sectionName", "subjectCode", "enrollmentType", "academicYear", "semester"],
            _ => throw new AppValidationEx($"Unsupported entity '{entity}'."),
        };
    }

    private static bool IsRequiredColumn(string entity, string column)
    {
        return entity switch
        {
            "users" => column is "username" or "email" or "role" or "temporaryPassword",
            "courses" => column == "name",
            "sections" => column is "name" or "courseName",
            "subjects" => column is "code" or "name",
            "classrooms" => column == "name",
            "schedules" => column is "dayOfWeek" or "timeIn" or "timeOut" or "subjectCode" or "sectionName" or "classroomName" or "instructorEmail",
            "enrollments" => column is "studentEmail" or "sectionName" or "subjectCode",
            _ => false,
        };
    }

    private static string BuildDuplicateKey(string entity, IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        return entity switch
        {
            "users" => $"{values.GetValueOrDefault("username")}|{values.GetValueOrDefault("email")}",
            "courses" => values.GetValueOrDefault("name") ?? string.Empty,
            "sections" => values.GetValueOrDefault("name") ?? string.Empty,
            "subjects" => $"{values.GetValueOrDefault("code")}|{values.GetValueOrDefault("name")}",
            "classrooms" => values.GetValueOrDefault("name") ?? string.Empty,
            "schedules" => BuildScheduleKey(values, lookup),
            "enrollments" => BuildEnrollmentKey(values, lookup),
            _ => string.Empty,
        };
    }

    private static string BuildScheduleKey(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var subject = lookup.SubjectIdsByCode.TryGetValue(values.GetValueOrDefault("subjectcode") ?? string.Empty, out var subjectId)
            ? subjectId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("subjectcode") ?? string.Empty;
        var section = lookup.SectionIdsByName.TryGetValue(values.GetValueOrDefault("sectionname") ?? string.Empty, out var sectionId)
            ? sectionId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("sectionname") ?? string.Empty;
        var classroom = lookup.ClassroomIdsByName.TryGetValue(values.GetValueOrDefault("classroomname") ?? string.Empty, out var classroomId)
            ? classroomId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("classroomname") ?? string.Empty;
        var instructor = lookup.InstructorIdsByEmail.TryGetValue(values.GetValueOrDefault("instructoremail") ?? string.Empty, out var instructorId)
            ? instructorId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("instructoremail") ?? string.Empty;

        return $"{NormalizeScheduleDayOfWeek(values.GetValueOrDefault("dayofweek"))}|{NormalizeScheduleTime(values.GetValueOrDefault("timein"))}|{NormalizeScheduleTime(values.GetValueOrDefault("timeout"))}|{subject}|{section}|{classroom}|{instructor}";
    }

    private static string BuildScheduleKey(string dayOfWeek, TimeOnly timeIn, TimeOnly timeOut, int subjectId, int sectionId, int classroomId, int instructorId)
        => $"{dayOfWeek}|{timeIn:HH\\:mm}|{timeOut:HH\\:mm}|{subjectId}|{sectionId}|{classroomId}|{instructorId}";

    private static string BuildEnrollmentKey(IReadOnlyDictionary<string, string?> values, LookupCache lookup)
    {
        var student = lookup.StudentIdsByEmail.TryGetValue(values.GetValueOrDefault("studentemail") ?? string.Empty, out var studentId)
            ? studentId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("studentemail") ?? string.Empty;
        var section = lookup.SectionIdsByName.TryGetValue(values.GetValueOrDefault("sectionname") ?? string.Empty, out var sectionId)
            ? sectionId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("sectionname") ?? string.Empty;
        var subject = lookup.SubjectIdsByCode.TryGetValue(values.GetValueOrDefault("subjectcode") ?? string.Empty, out var subjectId)
            ? subjectId.ToString(CultureInfo.InvariantCulture)
            : values.GetValueOrDefault("subjectcode") ?? string.Empty;

        return $"{student}|{section}|{subject}";
    }

    private static string BuildEnrollmentKey(int studentId, int sectionId, int subjectId)
        => $"{studentId}|{sectionId}|{subjectId}";

    private static Dictionary<string, string?> NormalizeRowValues(string entity, IReadOnlyDictionary<string, string?> row)
    {
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in GetColumns(entity))
        {
            var normalizedColumn = NormalizeColumnName(column);
            normalized[normalizedColumn] = row.TryGetValue(normalizedColumn, out var value)
                ? NullIfEmpty(value)
                : row.TryGetValue(column, out var originalValue)
                    ? NullIfEmpty(originalValue)
                    : null;
        }

        return normalized;
    }

    private static string NormalizeEntity(string entity)
    {
        var normalized = entity.Trim().ToLowerInvariant();
        return normalized switch
        {
            "users" or "courses" or "sections" or "subjects" or "classrooms" or "schedules" or "enrollments" => normalized,
            _ => throw new AppValidationEx($"Unsupported entity '{entity}'."),
        };
    }

    private static string NormalizeFormat(string format)
    {
        var normalized = format.Trim().ToLowerInvariant();
        if (!SupportedFormats.Contains(normalized))
        {
            throw new AppValidationEx($"Unsupported format '{format}'. Supported formats: csv, xlsx.");
        }

        return normalized;
    }

    private static string DetectFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!SupportedFormats.Contains(extension))
        {
            throw new AppValidationEx($"Unsupported file type '{extension}'. Supported file types: .csv, .xlsx.");
        }

        return extension;
    }

    private void EnsureValidFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new AppValidationEx("A non-empty file is required.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new AppValidationEx($"File size exceeds {_options.MaxFileSizeBytes / 1024 / 1024}MB limit.");
        }

        _ = DetectFormat(file.FileName);
    }

    private async Task<List<Dictionary<string, string?>>> ParseRowsAsync(IFormFile file, string format, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return format switch
        {
            "csv" => await ParseCsvAsync(stream, cancellationToken).ConfigureAwait(false),
            "xlsx" => ParseWorkbook(stream),
            _ => throw new AppValidationEx($"Unsupported format '{format}'."),
        };
    }

    private static async Task<List<Dictionary<string, string?>>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
        });

        if (!await csv.ReadAsync().ConfigureAwait(false) || !csv.ReadHeader())
        {
            throw new AppValidationEx("The uploaded file must include a header row.");
        }

        var headers = csv.HeaderRecord?
            .Select(header => header?.Trim() ?? string.Empty)
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToArray() ?? [];
        if (headers.Length == 0)
        {
            throw new AppValidationEx("The uploaded file must include at least one column.");
        }

        var rows = new List<Dictionary<string, string?>>();
        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[NormalizeColumnName(header)] = NullIfEmpty(csv.GetField(header));
            }
            rows.Add(row);
        }

        return rows;
    }

    private static List<Dictionary<string, string?>> ParseWorkbook(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => string.Equals(sheet.Name, "Data", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.First();
        var range = worksheet.RangeUsed();
        if (range == null)
        {
            throw new AppValidationEx("The uploaded workbook is empty.");
        }

        var headers = range.FirstRow().Cells()
            .Select(cell => cell.GetString().Trim())
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .Select(NormalizeColumnName)
            .ToArray();
        if (headers.Length == 0)
        {
            throw new AppValidationEx("The uploaded workbook must include a header row.");
        }

        var rows = new List<Dictionary<string, string?>>();
        foreach (var dataRow in range.RowsUsed().Skip(1))
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = NullIfEmpty(dataRow.Cell(index + 1).GetString());
            }
            rows.Add(row);
        }

        return rows;
    }

    private static async Task<byte[]> BuildCsvAsync(IReadOnlyList<string> columns, IReadOnlyCollection<IReadOnlyDictionary<string, string?>> rows, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var column in columns)
        {
            csv.WriteField(column);
        }
        await csv.NextRecordAsync().ConfigureAwait(false);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var column in columns)
            {
                csv.WriteField(row.TryGetValue(column, out var value) ? value : null);
            }
            await csv.NextRecordAsync().ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    private static byte[] BuildWorkbook(IReadOnlyList<string> columns, IReadOnlyCollection<IReadOnlyDictionary<string, string?>> rows, string entity, bool includeInstructions)
    {
        using var workbook = new XLWorkbook();
        var dataSheet = workbook.Worksheets.Add("Data");
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            dataSheet.Cell(1, columnIndex + 1).Value = columns[columnIndex];
            dataSheet.Cell(1, columnIndex + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                dataSheet.Cell(rowIndex, columnIndex + 1).Value = row.TryGetValue(columns[columnIndex], out var value) ? value : null;
            }
            rowIndex++;
        }

        dataSheet.Columns().AdjustToContents();

        if (includeInstructions)
        {
            var instructionSheet = workbook.Worksheets.Add("Instructions");
            instructionSheet.Cell(1, 1).Value = $"{entity} import template";
            instructionSheet.Cell(2, 1).Value = "Fill the Data sheet and keep headers unchanged.";
            instructionSheet.Cell(3, 1).Value = "Preview import before committing; duplicate rows are skipped.";
            instructionSheet.Column(1).AdjustToContents();
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private static AdminDataRowResultDto CloneRow(AdminDataRowResultDto row)
        => new()
        {
            RowNumber = row.RowNumber,
            Status = row.Status,
            Values = new Dictionary<string, string?>(row.Values, StringComparer.OrdinalIgnoreCase),
            Issues = row.Issues.Select(CloneIssue).ToList(),
        };

    private static AdminDataIssueDto CloneIssue(AdminDataIssueDto issue)
        => new()
        {
            RowNumber = issue.RowNumber,
            Code = issue.Code,
            Severity = issue.Severity,
            Message = issue.Message,
            Field = issue.Field,
        };

    private static AdminDataIssueDto CreateIssue(int? rowNumber, string code, string severity, string message, string? field = null)
        => new()
        {
            RowNumber = rowNumber,
            Code = code,
            Severity = severity,
            Message = message,
            Field = field,
        };

    private void ApplyIssueLimit(List<AdminDataIssueDto> fileIssues, List<AdminDataRowResultDto> rows)
    {
        var remainingIssues = Math.Max(_options.MaxIssues, 0);
        var originalFileIssues = fileIssues.Select(CloneIssue).ToList();
        var originalRowIssues = rows.ToDictionary(
            row => row,
            row => row.Issues.Select(CloneIssue).ToList());

        fileIssues.Clear();
        foreach (var row in rows)
        {
            row.Issues.Clear();
        }

        if (remainingIssues == 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            var issues = originalRowIssues[row];
            if (issues.Count == 0 || remainingIssues == 0)
            {
                continue;
            }

            row.Issues.Add(issues[0]);
            issues.RemoveAt(0);
            remainingIssues--;
        }

        foreach (var issue in originalFileIssues)
        {
            if (remainingIssues == 0)
            {
                break;
            }

            fileIssues.Add(issue);
            remainingIssues--;
        }

        foreach (var row in rows)
        {
            foreach (var issue in originalRowIssues[row])
            {
                if (remainingIssues == 0)
                {
                    return;
                }

                row.Issues.Add(issue);
                remainingIssues--;
            }
        }
    }

    private static int ResolveLookup(IReadOnlyDictionary<string, int> lookup, AdminDataRowResultDto row, string key, string entityName)
    {
        var value = row.Values.GetValueOrDefault(key);
        if (!string.IsNullOrWhiteSpace(value) && lookup.TryGetValue(value, out var resolvedId))
        {
            return resolvedId;
        }

        row.Issues.Add(CreateIssue(row.RowNumber, "missing_reference", "error", $"{entityName} '{value}' was not found.", key));
        return 0;
    }

    private static TimeOnly ParseTime(AdminDataRowResultDto row, string key)
    {
        try
        {
            return ParseTime(row.Values.GetValueOrDefault(key));
        }
        catch (AppValidationEx ex)
        {
            row.Issues.Add(CreateIssue(row.RowNumber, "invalid_time", "error", ex.Message, key));
            return TimeOnly.MinValue;
        }
    }

    private static TimeOnly ParseTime(string? value)
    {
        if (TimeOnly.TryParseExact(value, ["HH:mm", "H:mm", "hh:mm tt", "h:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        throw new AppValidationEx($"Invalid time value '{value}'. Use HH:mm or h:mm tt.");
    }

    private static string NormalizeScheduleTime(string? value)
    {
        var normalizedValue = NullIfEmpty(value);
        if (TimeOnly.TryParseExact(normalizedValue, ["HH:mm", "H:mm", "hh:mm tt", "h:mm tt"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time.ToString("HH\\:mm", CultureInfo.InvariantCulture);
        }

        return normalizedValue ?? string.Empty;
    }

    private static string NormalizeScheduleDayOfWeek(string? value)
    {
        var normalizedValue = NullIfEmpty(value);
        if (normalizedValue == null)
        {
            return string.Empty;
        }

        return ScheduleConstants.ValidDaysOfWeek.FirstOrDefault(day => string.Equals(day, normalizedValue, StringComparison.OrdinalIgnoreCase))
            ?? normalizedValue;
    }

    private static bool MatchesUserExportFilters(GetAllUsersDto user, string? roleFilter, string? searchFilter)
    {
        if (!string.IsNullOrWhiteSpace(roleFilter) && !string.Equals(roleFilter, "All Roles", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(NormalizeRole(user.Role), NormalizeRole(roleFilter), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var query = NullIfEmpty(searchFilter)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var firstName = (user.StudentProfile?.Firstname ?? user.InstructorProfile?.Firstname ?? user.AdminProfile?.Firstname ?? string.Empty).ToLowerInvariant();
        var lastName = (user.StudentProfile?.Lastname ?? user.InstructorProfile?.Lastname ?? user.AdminProfile?.Lastname ?? string.Empty).ToLowerInvariant();
        var email = (user.Email ?? string.Empty).ToLowerInvariant();
        var username = (user.Username ?? string.Empty).ToLowerInvariant();

        return firstName.Contains(query, StringComparison.Ordinal)
            || lastName.Contains(query, StringComparison.Ordinal)
            || email.Contains(query, StringComparison.Ordinal)
            || username.Contains(query, StringComparison.Ordinal)
            || $"{firstName} {lastName}".Contains(query, StringComparison.Ordinal);
    }

    private static string NormalizeRole(string? role)
        => string.IsNullOrWhiteSpace(role) ? RoleConstants.Student : RoleConstants.NormalizeRole(role);

    private static string NormalizeColumnName(string column)
        => new string(column.Where(ch => ch != ' ' && ch != '_' && ch != '-').ToArray()).ToLowerInvariant();

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ImportAnalysisResult(
        string Entity,
        string Format,
        string FileName,
        int TotalRows,
        IReadOnlyList<string> Columns,
        IReadOnlyList<AdminDataIssueDto> FileIssues,
        IReadOnlyList<AdminDataRowResultDto> Rows,
        int ReadyRows,
        int DuplicateRows,
        int InvalidRows,
        bool CanImport,
        LookupCache Lookup);

    private sealed record LookupCache(
        IReadOnlyDictionary<string, int> CourseIdsByName,
        IReadOnlyDictionary<string, int> SectionIdsByName,
        IReadOnlyDictionary<string, int> SubjectIdsByCode,
        IReadOnlyDictionary<string, int> ClassroomIdsByName,
        IReadOnlyDictionary<string, int> InstructorIdsByEmail,
        IReadOnlyDictionary<string, int> StudentIdsByEmail,
        IReadOnlyDictionary<int, int> StudentPrimarySectionIds,
        IReadOnlySet<string> ExistingUsernames,
        IReadOnlySet<string> ExistingEmails,
        IReadOnlySet<string> ExistingCourseNames,
        IReadOnlySet<string> ExistingSectionNames,
        IReadOnlySet<string> ExistingSubjectKeys,
        IReadOnlySet<string> ExistingClassroomNames,
        IReadOnlySet<string> ExistingScheduleKeys,
        IReadOnlySet<string> ExistingEnrollmentKeys);
}
