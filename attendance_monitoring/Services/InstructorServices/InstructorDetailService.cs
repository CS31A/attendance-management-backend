using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.InstructorServices;

/// <summary>
/// Service for instructor section detail, student detail, and section overview queries
/// </summary>
public class InstructorDetailService : IInstructorDetailService
{
    private readonly IInstructorRepository _instructorRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IFingerprintRepository _fingerprintRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<InstructorDetailService> _logger;

    public InstructorDetailService(
        IInstructorRepository instructorRepository,
        ISectionRepository sectionRepository,
        IStudentRepository studentRepository,
        IScheduleRepository scheduleRepository,
        IFingerprintRepository fingerprintRepository,
        IUserContextService userContextService,
        ILogger<InstructorDetailService> logger)
    {
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _fingerprintRepository = fingerprintRepository ?? throw new ArgumentNullException(nameof(fingerprintRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region GetInstructorSectionsOverviewAsync
    /// <summary>
    /// Retrieves a high-level overview of all sections handled by the authenticated instructor.
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>A list of section overviews with class counts and student counts</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Retrieving sections overview for authenticated instructor");

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                throw new EntityNotFoundException<string>("User", userId ?? "null", "User identity not found in request");
            }

            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}", "Instructor not found for the current user");
            }

            _logger.LogInformation("Getting handled sections for instructor ID: {InstructorId}", instructor.Id);

            var sections = await _instructorRepository.GetHandledSectionsByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
            var sectionList = sections.ToList();

            var overviewDtos = new List<InstructorSectionOverviewDto>();

            foreach (var section in sectionList)
            {
                var schedules = await _instructorRepository.GetHandledClassesBySectionAndInstructorAsync(section.Id, instructor.Id).ConfigureAwait(false);
                var schedulesList = schedules.ToList();
                var handledSubjectIds = schedulesList
                    .Select(schedule => schedule.SubjectId)
                    .Distinct()
                    .ToHashSet();
                var handledClassCount = handledSubjectIds.Count;

                var uniqueStudentCount = 0;
                if (handledClassCount > 0)
                {
                    var regularStudents = await _instructorRepository.GetRegularStudentsBySectionIdAsync(section.Id).ConfigureAwait(false);
                    var regularStudentIds = regularStudents.Select(student => student.Id);
                    var sectionWithEnrollments = schedulesList.First().Section;
                    var irregularStudentIds = sectionWithEnrollments.StudentEnrollments
                        .Where(enrollment => handledSubjectIds.Contains(enrollment.SubjectId)
                            && enrollment.IsActive
                            && !enrollment.Student.IsDeleted
                            && enrollment.Student.SectionId != section.Id)
                        .Select(enrollment => enrollment.StudentId);

                    uniqueStudentCount = regularStudentIds
                        .Concat(irregularStudentIds)
                        .Distinct()
                        .Count();
                }

                overviewDtos.Add(new InstructorSectionOverviewDto
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    CourseId = SectionHelper.GetRequiredCourse(section).Id,
                    CourseName = SectionHelper.GetRequiredCourse(section).Name,
                    HandledClassCount = handledClassCount,
                    UniqueStudentCount = uniqueStudentCount
                });
            }

            _logger.LogInformation("Successfully retrieved {SectionCount} section overviews for instructor ID: {InstructorId}",
                overviewDtos.Count, instructor.Id);

            return overviewDtos.OrderBy(s => s.SectionName).ToList();
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sections overview for authenticated instructor");
            throw new EntityServiceException("Instructor", "GetInstructorSectionsOverview",
                "An error occurred while retrieving sections overview", ex);
        }
    }
    #endregion

    #region GetInstructorSectionDetailAsync
    /// <summary>
    /// Retrieves detailed information about a specific section handled by the authenticated instructor.
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <param name="sectionId">The section ID to retrieve details for</param>
    /// <returns>Detailed section information including handled classes and home section students</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when the instructor is not authorized to view the section</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal userPrincipal, Guid sectionId)
    {
        try
        {
            _logger.LogInformation("Retrieving section detail for section ID: {SectionId}", sectionId);

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                throw new EntityNotFoundException<string>("User", userId ?? "null", "User identity not found in request");
            }

            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}", "Instructor not found for the current user");
            }

            var sectionExists = await _sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
            if (sectionExists == null)
            {
                _logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                throw new EntityNotFoundException<Guid>("Section", sectionId);
            }

            var isHandlingSection = await _instructorRepository.IsInstructorHandlingSectionAsync(instructor.Id, sectionId).ConfigureAwait(false);
            if (!isHandlingSection)
            {
                _logger.LogWarning("Instructor ID {InstructorId} is not authorized to view section ID: {SectionId}", instructor.Id, sectionId);
                throw new EntityUnauthorizedException("Section", $"View section with ID {sectionId}", userId, "You are not authorized to view this section");
            }

            var schedules = await _instructorRepository.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructor.Id).ConfigureAwait(false);
            var schedulesList = schedules.ToList();

            var section = schedulesList.FirstOrDefault()?.Section;
            if (section == null)
            {
                var handledSections = await _instructorRepository.GetHandledSectionsByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
                section = handledSections.FirstOrDefault(s => s.Id == sectionId);
            }

            if (section == null)
            {
                _logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                throw new EntityNotFoundException<Guid>("Section", sectionId);
            }

            var handledClasses = new List<InstructorHandledClassDto>();
            var regularStudentEntities = (await _instructorRepository.GetRegularStudentsBySectionIdAsync(sectionId).ConfigureAwait(false)).ToList();
            var homeSectionStudentEntities = (await _instructorRepository.GetHomeSectionStudentsAsync(sectionId).ConfigureAwait(false)).ToList();
            var irregularStudentEntities = schedulesList
                .SelectMany(schedule => section.StudentEnrollments
                    .Where(se => se.SubjectId == schedule.SubjectId
                        && !se.Student.IsDeleted
                        && se.IsActive
                        && se.Student.SectionId != sectionId)
                    .Select(se => se.Student))
                .ToList();
            var fingerprintLookup = await BuildFingerprintLookupAsync(regularStudentEntities
                .Concat(homeSectionStudentEntities)
                .Concat(irregularStudentEntities))
                .ConfigureAwait(false);

            var regularStudents = regularStudentEntities
                .Select(student => CreateHandledClassStudentDto(
                    student,
                    true,
                    EnrollmentTypeConstants.Regular,
                    fingerprintLookup))
                .ToList();

            foreach (var scheduleGroup in schedulesList.GroupBy(s => s.SubjectId))
            {
                var schedule = scheduleGroup.First();

                var irregularStudents = section.StudentEnrollments
                    .Where(se => se.SubjectId == schedule.SubjectId
                        && !se.Student.IsDeleted
                        && se.IsActive
                        && se.Student.SectionId != sectionId)
                    .Select(se => CreateHandledClassStudentDto(
                        se.Student,
                        false,
                        se.EnrollmentType,
                        fingerprintLookup))
                    .ToList();

                var allStudents = regularStudents
                    .Concat(irregularStudents)
                    .GroupBy(s => s.StudentId)
                    .Select(g => g.First())
                    .OrderBy(s => s.Lastname)
                    .ThenBy(s => s.Firstname)
                    .ToList();

                handledClasses.Add(new InstructorHandledClassDto
                {
                    SubjectId = schedule.Subject.Id,
                    SubjectName = schedule.Subject.Name,
                    SubjectCode = schedule.Subject.Code,
                    ScheduleId = schedule.Id,
                    DayOfWeek = schedule.DayOfWeek,
                    TimeIn = schedule.TimeIn,
                    TimeOut = schedule.TimeOut,
                    ClassroomId = schedule.Classroom.Id,
                    ClassroomName = schedule.Classroom.Name,
                    StudentCount = allStudents.Count,
                    Students = allStudents
                });
            }

            var homeSectionStudentDtos = homeSectionStudentEntities
                .Select(student => CreateHomeSectionStudentDto(student, sectionId, fingerprintLookup))
                .ToList();

            var detailDto = new InstructorSectionDetailDto
            {
                SectionId = section.Id,
                SectionName = section.Name,
                CourseId = SectionHelper.GetRequiredCourse(section).Id,
                CourseName = SectionHelper.GetRequiredCourse(section).Name,
                HandledClassCount = handledClasses.Count,
                HomeSectionStudentCount = homeSectionStudentDtos.Count,
                HandledClasses = handledClasses.OrderBy(h => h.SubjectName).ToList(),
                HomeSectionStudents = homeSectionStudentDtos.OrderBy(s => s.Lastname).ThenBy(s => s.Firstname).ToList()
            };

            _logger.LogInformation("Successfully retrieved section detail for section ID: {SectionId}, instructor ID: {InstructorId}",
                sectionId, instructor.Id);

            return detailDto;
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving section detail for section ID: {SectionId}", sectionId);
            throw new EntityServiceException("Instructor", $"GetInstructorSectionDetail: {sectionId}",
                "An error occurred while retrieving section detail", ex);
        }
    }
    #endregion

    public async Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal userPrincipal, Guid sectionUuid)
    {
        var section = await _sectionRepository.GetSectionByUuidAsync(sectionUuid).ConfigureAwait(false);
        if (section == null)
        {
            throw new EntityNotFoundException<Guid>("Section", sectionUuid);
        }

        return await GetInstructorSectionDetailAsync(userPrincipal, section.Id).ConfigureAwait(false);
    }

    #region GetInstructorStudentDetailAsync
    /// <summary>
    /// Retrieves detailed information about a specific student visible to the authenticated instructor.
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <param name="studentId">The student ID to retrieve details for</param>
    /// <returns>Detailed student information including enrollments and attendance summary</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the student is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when the instructor is not authorized to view the student</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal userPrincipal, Guid studentId)
    {
        try
        {
            _logger.LogInformation("Retrieving student detail for student ID: {StudentId}", studentId);

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                throw new EntityNotFoundException<string>("User", userId ?? "null", "User identity not found in request");
            }

            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}", "Instructor not found for the current user");
            }

            var student = await _instructorRepository.GetStudentWithDetailsAsync(studentId).ConfigureAwait(false);
            if (student == null)
            {
                _logger.LogWarning("Student with ID {StudentId} not found", studentId);
                throw new EntityNotFoundException<Guid>("Student", studentId);
            }

            var isStudentVisible = await IsStudentVisibleToInstructorAsync(instructor.Id, student).ConfigureAwait(false);
            if (!isStudentVisible)
            {
                _logger.LogWarning("Instructor ID {InstructorId} is not authorized to view student ID: {StudentId}", instructor.Id, studentId);
                throw new EntityUnauthorizedException("Student", $"View student with ID {studentId}", userId, "You are not authorized to view this student");
            }

            // Build home-section enrollments from schedules taught by this instructor
            var instructorSchedules = await _instructorRepository
                .GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructor.Id)
                .ConfigureAwait(false);

            var homeSectionEnrollments = instructorSchedules
                .GroupBy(schedule => schedule.SubjectId)
                .Select(group => group.First())
                .Select(schedule => new InstructorStudentEnrollmentDto
                {
                    SubjectId = schedule.Subject.Id,
                    SubjectName = schedule.Subject.Name,
                    SubjectCode = schedule.Subject.Code,
                    SectionId = schedule.Section.Id,
                    SectionName = schedule.Section.Name,
                    EnrollmentType = EnrollmentTypeConstants.Regular
                });

            var additionalEnrollments = student.AdditionalEnrollments
                .Where(se => se.IsActive)
                .Select(se => new InstructorStudentEnrollmentDto
                {
                    SubjectId = se.Subject.Id,
                    SubjectName = se.Subject.Name,
                    SubjectCode = se.Subject.Code,
                    SectionId = se.Section.Id,
                    SectionName = se.Section.Name,
                    EnrollmentType = se.EnrollmentType
                });

            var enrollments = homeSectionEnrollments
                .Concat(additionalEnrollments)
                .ToList();

            var attendanceRecords = await _instructorRepository.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructor.Id).ConfigureAwait(false);
            var attendanceList = attendanceRecords.ToList();

            var totalSessions = attendanceList.Count;
            var presentCount = attendanceList.Count(ar => ar.Status == AttendanceStatusConstants.Present);
            var absentCount = attendanceList.Count(ar => ar.Status == AttendanceStatusConstants.Absent);
            var lateCount = attendanceList.Count(ar => ar.Status == AttendanceStatusConstants.Late);
            var attendanceRate = totalSessions > 0 ? (double)(presentCount + lateCount) / totalSessions * 100 : 0;

            var fingerprint = await _fingerprintRepository.GetFingerprintByStudentIdAsync(studentId).ConfigureAwait(false);
            InstructorStudentFingerprintDto? fingerprintDto = null;
            if (fingerprint != null && !fingerprint.IsDeleted)
            {
                var devices = await _fingerprintRepository.GetDevicesAsync().ConfigureAwait(false);
                var deviceLookup = BuildDeviceLookup(devices);
                deviceLookup.TryGetValue(fingerprint.DeviceId, out var device);

                fingerprintDto = new InstructorStudentFingerprintDto
                {
                    Id = fingerprint.Id,
                    DeviceId = fingerprint.DeviceId,
                    DeviceName = device?.Name ?? fingerprint.DeviceId,
                    DeviceLocation = device?.Location ?? string.Empty,
                    EnrolledAt = fingerprint.CreatedAt
                };
            }

            var detailDto = new InstructorStudentDetailDto
            {
                StudentId = student.Id,
                Firstname = student.Firstname,
                Lastname = student.Lastname,
                SectionId = student.Section?.Id,
                SectionName = student.Section?.Name,
                CourseId = student.Section?.Course?.Id,
                CourseName = student.Section?.Course?.Name,
                IsRegular = student.IsRegular,
                EnrollmentType = student.IsRegular ? EnrollmentTypeConstants.Regular : EnrollmentTypeConstants.Irregular,
                Enrollments = enrollments,
                AttendanceSummary = new InstructorStudentAttendanceSummaryDto
                {
                    TotalSessions = totalSessions,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    AttendanceRate = Math.Round(attendanceRate, 2)
                },
                Fingerprint = fingerprintDto
            };

            _logger.LogInformation("Successfully retrieved student detail for student ID: {StudentId}, instructor ID: {InstructorId}",
                studentId, instructor.Id);

            return detailDto;
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving student detail for student ID: {StudentId}", studentId);
            throw new EntityServiceException("Instructor", $"GetInstructorStudentDetail: {studentId}",
                "An error occurred while retrieving student detail", ex);
        }
    }

    private async Task<bool> IsStudentVisibleToInstructorAsync(Guid instructorId, Student student)
    {
        var isHandlingStudentSection = await _instructorRepository.IsInstructorHandlingSectionAsync(instructorId, student.SectionId).ConfigureAwait(false);
        if (isHandlingStudentSection)
        {
            return true;
        }

        var instructorSchedules = await _scheduleRepository.GetSchedulesByInstructorIdAsync(instructorId).ConfigureAwait(false);
        var handledSectionSubjectPairs = instructorSchedules
            .Select(schedule => (schedule.SectionId, schedule.SubjectId))
            .ToHashSet();

        return student.AdditionalEnrollments.Any(studentEnrollment =>
            studentEnrollment.IsActive
            && handledSectionSubjectPairs.Contains((studentEnrollment.SectionId, studentEnrollment.SubjectId)));
    }
    #endregion

    public async Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal userPrincipal, Guid studentUuid)
    {
        var student = await _studentRepository.GetStudentByUuidAsync(studentUuid).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<Guid>("Student", studentUuid);
        }

        return await GetInstructorStudentDetailAsync(userPrincipal, student.Id).ConfigureAwait(false);
    }

    #region Helper Methods

    private async Task<Dictionary<Guid, StudentFingerprintDisplay>> BuildFingerprintLookupAsync(IEnumerable<Student> students)
    {
        var studentList = students
            .GroupBy(student => student.Id)
            .Select(group => group.First())
            .ToList();
        var studentUserIds = studentList
            .Select(student => student.UserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .ToHashSet(StringComparer.Ordinal);

        if (studentUserIds.Count == 0)
        {
            return new Dictionary<Guid, StudentFingerprintDisplay>();
        }

        var fingerprints = await _fingerprintRepository.GetActiveFingerprintsAsync().ConfigureAwait(false);
        var devices = await _fingerprintRepository.GetDevicesAsync().ConfigureAwait(false);
        var deviceLookup = BuildDeviceLookup(devices);
        var fingerprintByUserId = fingerprints
            .Where(fingerprint => studentUserIds.Contains(fingerprint.UserId))
            .GroupBy(fingerprint => fingerprint.UserId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return studentList
            .Where(student => fingerprintByUserId.ContainsKey(student.UserId))
            .ToDictionary(
                student => student.Id,
                student =>
                {
                    var fingerprint = fingerprintByUserId[student.UserId];
                    deviceLookup.TryGetValue(fingerprint.DeviceId, out var device);
                    return new StudentFingerprintDisplay(
                        fingerprint.DeviceId,
                        device?.Name);
                });
    }

    private static Dictionary<string, FingerprintDevice> BuildDeviceLookup(IEnumerable<FingerprintDevice> devices)
    {
        return devices
            .Where(device => !string.IsNullOrWhiteSpace(device.DeviceIdentifier))
            .GroupBy(device => device.DeviceIdentifier, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static InstructorHandledClassStudentDto CreateHandledClassStudentDto(
        Student student,
        bool isRegular,
        string enrollmentType,
        IReadOnlyDictionary<Guid, StudentFingerprintDisplay> fingerprintLookup)
    {
        fingerprintLookup.TryGetValue(student.Id, out var fingerprint);

        return new InstructorHandledClassStudentDto
        {
            StudentId = student.Id,
            Firstname = student.Firstname,
            Lastname = student.Lastname,
            IsRegular = isRegular,
            EnrollmentType = enrollmentType,
            HasFingerprint = fingerprint != null,
            FingerprintDeviceId = fingerprint?.DeviceId,
            FingerprintDeviceName = fingerprint?.DeviceName
        };
    }

    private static InstructorHomeSectionStudentDto CreateHomeSectionStudentDto(
        Student student,
        Guid sectionId,
        IReadOnlyDictionary<Guid, StudentFingerprintDisplay> fingerprintLookup)
    {
        fingerprintLookup.TryGetValue(student.Id, out var fingerprint);

        return new InstructorHomeSectionStudentDto
        {
            StudentId = student.Id,
            Firstname = student.Firstname,
            Lastname = student.Lastname,
            IsRegular = student.SectionId == sectionId,
            EnrollmentType = student.SectionId == sectionId ? EnrollmentTypeConstants.Regular : EnrollmentTypeConstants.Irregular,
            HasFingerprint = fingerprint != null,
            FingerprintDeviceId = fingerprint?.DeviceId,
            FingerprintDeviceName = fingerprint?.DeviceName
        };
    }

    private sealed record StudentFingerprintDisplay(string DeviceId, string? DeviceName);

    #endregion
}
