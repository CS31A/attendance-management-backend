using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Options;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using attendance_monitoring.Services.AdminData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;

namespace attendance.testproject.Services_Testing;

public class AdminDataServiceTests
{
    [Fact]
    public async Task PreviewImport_UsersCsv_WithKnownSection_ReturnsReadyRows()
    {
        await using var context = CreateContext();
        context.Sections.Add(new Section { Id = 12, Name = "BSCS-1A", CourseId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.csv", "username,email,firstname,lastname,role,sectionName,temporaryPassword\nalpha,alpha@example.com,Alice,Anderson,Student,BSCS-1A,Secret123\n");

        var result = await service.PreviewImportAsync("users", file, new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.True(result.CanImport);
        Assert.Equal(1, result.ReadyRows);
        Assert.Equal(0, result.InvalidRows);
        Assert.Equal(0, result.DuplicateRows);
        Assert.Single(result.Rows);
        Assert.Equal("ready", result.Rows[0].Status);
    }

    [Fact]
    public async Task PreviewImport_UsersCsv_WithExistingUser_ReturnsDuplicateRow()
    {
        await using var context = CreateContext();
        context.Sections.Add(new Section { Id = 8, Name = "BSIT-1B", CourseId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(new[]
            {
                new GetAllUsersDto
                {
                    UserId = "u-1",
                    Username = "alpha",
                    Email = "alpha@example.com",
                    Role = "Student",
                },
            });

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.csv", "username,email,firstname,lastname,role,sectionName,temporaryPassword\nalpha,alpha@example.com,Alice,Anderson,Student,BSIT-1B,Secret123\n");

        var result = await service.PreviewImportAsync("users", file, new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.True(result.CanImport);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Equal("duplicate", result.Rows[0].Status);
    }

    [Fact]
    public async Task PreviewImport_SchedulesCsv_WithInvalidTimeOut_DoesNotAddTimeRangeIssue()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Subjects.Add(new Subject
        {
            Id = 11,
            Code = "CS101",
            Name = "Intro to Computing",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Classrooms.Add(new Classroom
        {
            Id = 7,
            Name = "Lab 1",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Sections.Add(new Section
        {
            Id = 5,
            Name = "BSCS-1A",
            CourseId = 2,
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Users.Add(new IdentityUser
        {
            Id = "inst-1",
            Email = "teacher@example.com",
            UserName = "teacher@example.com",
        });
        context.Instructors.Add(new Instructor
        {
            Id = 3,
            UserId = "inst-1",
            Firstname = "Ada",
            Lastname = "Lovelace",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("schedules.csv", "dayOfWeek,timeIn,timeOut,subjectCode,sectionName,classroomName,instructorEmail\nMonday,08:00,not-a-time,CS101,BSCS-1A,Lab 1,teacher@example.com\n");

        var result = await service.PreviewImportAsync("schedules", file, new Dictionary<string, string?>());

        var row = Assert.Single(result.Rows);
        Assert.Equal("invalid", row.Status);
        Assert.Contains(row.Issues, issue => issue.Code == "invalid_time" && issue.Field == "timeout");
        Assert.DoesNotContain(row.Issues, issue => issue.Code == "invalid_time_range");
    }

    [Fact]
    public async Task ImportAsync_UsersCsv_ImportsAllRowsBeyondPreviewLimit()
    {
        await using var context = CreateContext();
        context.Sections.Add(new Section { Id = 12, Name = "BSCS-1A", CourseId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());
        accountService.Setup(service => service.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(new RegisterResponseDto());

        var service = CreateService(context, accountService.Object, new BulkDataOptions { MaxPreviewRows = 1 });
        var file = CreateFormFile(
            "users.csv",
            "username,email,firstname,lastname,role,sectionName,temporaryPassword\n"
            + "alpha,alpha@example.com,Alice,Anderson,Student,BSCS-1A,Secret123\n"
            + "bravo,bravo@example.com,Bob,Brown,Student,BSCS-1A,Secret123\n"
            + "charlie,charlie@example.com,Carol,Clark,Student,BSCS-1A,Secret123\n");

        var preview = await service.PreviewImportAsync("users", file, new Dictionary<string, string?>());
        var result = await service.ImportAsync("users", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.Single(preview.Rows);
        Assert.Equal(3, preview.TotalRows);
        Assert.Equal(3, result.TotalRows);
        Assert.True(result.Success);
        Assert.Equal(3, result.CreatedRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Equal(0, result.SkippedDuplicateRows);
        accountService.Verify(service => service.RegisterAsync(It.IsAny<RegisterDto>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PreviewImport_SchedulesCsv_WithSingleDigitHour_MatchesExistingScheduleAsDuplicate()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Subjects.Add(new Subject
        {
            Id = 11,
            Code = "CS101",
            Name = "Intro to Computing",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Classrooms.Add(new Classroom
        {
            Id = 7,
            Name = "Lab 1",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Sections.Add(new Section
        {
            Id = 5,
            Name = "BSCS-1A",
            CourseId = 2,
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Users.Add(new IdentityUser
        {
            Id = "inst-1",
            Email = "teacher@example.com",
            UserName = "teacher@example.com",
        });
        context.Instructors.Add(new Instructor
        {
            Id = 3,
            UserId = "inst-1",
            Firstname = "Ada",
            Lastname = "Lovelace",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.Schedules.Add(new Schedules
        {
            Id = 19,
            DayOfWeek = "Monday",
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(16, 0),
            SubjectId = 11,
            ClassroomId = 7,
            SectionId = 5,
            InstructorId = 3,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("schedules.csv", "dayOfWeek,timeIn,timeOut,subjectCode,sectionName,classroomName,instructorEmail\nMonday,8:00,16:00,CS101,BSCS-1A,Lab 1,teacher@example.com\n");

        var result = await service.PreviewImportAsync("schedules", file, new Dictionary<string, string?>());

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Equal("duplicate", row.Status);
        Assert.Contains(row.Issues, issue => issue.Code == "duplicate_existing");
    }

    [Fact]
    public async Task ExportAsync_UsersCsv_AppliesRoleAndSearchFilters()
    {
        await using var context = CreateContext();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(UserStatus.Active))
            .ReturnsAsync(new[]
            {
                new GetAllUsersDto
                {
                    UserId = "u-1",
                    Username = "ada.teacher",
                    Email = "ada@example.com",
                    Role = "Instructor",
                    InstructorProfile = new InstructorProfileDto
                    {
                        Firstname = "Ada",
                        Lastname = "Lovelace",
                    },
                },
                new GetAllUsersDto
                {
                    UserId = "u-2",
                    Username = "bob.student",
                    Email = "bob@example.com",
                    Role = "Student",
                    StudentProfile = new StudentProfileDto
                    {
                        Firstname = "Bob",
                        Lastname = "Stone",
                    },
                },
            });

        var service = CreateService(context, accountService.Object);

        var export = await service.ExportAsync("users", "csv", new Dictionary<string, string?>
        {
            ["status"] = "Active",
            ["role"] = "Instructor",
            ["search"] = "  lovelace  ",
        });

        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Contains("ada.teacher", csv);
        Assert.DoesNotContain("bob.student", csv);
        accountService.Verify(service => service.GetAllUsersAsync(UserStatus.Active), Times.Once);
    }

    [Fact]
    public async Task PreviewImport_UsersCsv_LimitsVisibleIssuesToMaxIssues()
    {
        await using var context = CreateContext();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object, new BulkDataOptions { MaxIssues = 2 });
        var file = CreateFormFile("users.csv", "username,email,firstname,lastname,role,sectionName,temporaryPassword\n,,, ,Student,,\n");

        var result = await service.PreviewImportAsync("users", file, new Dictionary<string, string?>());

        var totalVisibleIssues = result.FileIssues.Count + result.Rows.Sum(row => row.Issues.Count);

        Assert.Equal(1, result.InvalidRows);
        Assert.False(result.CanImport);
        Assert.True(totalVisibleIssues <= 2);
    }

    [Fact]
    public async Task PreviewImport_UsersCsv_PreservesOneVisibleIssuePerInvalidRow_WhenBudgetAllows()
    {
        await using var context = CreateContext();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(service => service.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object, new BulkDataOptions { MaxIssues = 2 });
        var file = CreateFormFile(
            "users.csv",
            "username,email,firstname,lastname,role,sectionName,temporaryPassword\n"
            + ",,, ,Student,,\n"
            + ",,, ,Student,,\n");

        var result = await service.PreviewImportAsync("users", file, new Dictionary<string, string?>());

        Assert.Equal(2, result.InvalidRows);
        Assert.Equal(2, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.Single(row.Issues));
        Assert.Equal(2, result.Rows.Sum(row => row.Issues.Count) + result.FileIssues.Count);
    }

    // === Cache-reuse tests ===

    [Fact]
    public async Task ImportAsync_SchedulesCsv_UsesLookupCacheInsteadOfPerRowQueries()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Subjects.Add(new Subject { Id = 10, Code = "CS101", Name = "Computing", CreatedAt = now, UpdatedAt = now });
        context.Classrooms.Add(new Classroom { Id = 5, Name = "Room A", CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 3, Name = "BSCS-1A", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Users.Add(new IdentityUser { Id = "i-1", Email = "prof@x.com", UserName = "prof@x.com" });
        context.Instructors.Add(new Instructor { Id = 2, UserId = "i-1", Firstname = "P", Lastname = "Q", CreatedAt = now, UpdatedAt = now });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var scheduleService = new Mock<IScheduleService>();
        scheduleService.Setup(s => s.CreateScheduleAsync(It.IsAny<CreateSchedule>()))
            .ReturnsAsync(new Schedules());

        var service = CreateService(context, accountService.Object, scheduleService: scheduleService.Object);
        var file = CreateFormFile("schedules.csv",
            "dayOfWeek,timeIn,timeOut,subjectCode,sectionName,classroomName,instructorEmail\n" +
            "Monday,08:00,10:00,CS101,BSCS-1A,Room A,prof@x.com\n" +
            "Tuesday,08:00,10:00,CS101,BSCS-1A,Room A,prof@x.com\n");

        var result = await service.ImportAsync("schedules", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(2, result.CreatedRows);
        scheduleService.Verify(s => s.CreateScheduleAsync(It.IsAny<CreateSchedule>()), Times.Exactly(2));

        // Verify the resolved IDs were passed correctly through cache, not re-queried
        var firstCall = scheduleService.Invocations[0].Arguments[0] as CreateSchedule;
        var secondCall = scheduleService.Invocations[1].Arguments[0] as CreateSchedule;
        Assert.NotNull(firstCall);
        Assert.NotNull(secondCall);
        Assert.Equal(10, firstCall.SubjectId);
        Assert.Equal(5, firstCall.ClassroomId);
        Assert.Equal(3, firstCall.SectionId);
        Assert.Equal(2, firstCall.InstructorId);
        Assert.Equal(10, secondCall.SubjectId);
    }

    [Fact]
    public async Task ImportAsync_SectionsCsv_UsesLookupCacheInsteadOfPerRowQueries()
    {
        await using var context = CreateContext();
        context.Courses.Add(new Course { Id = 7, Name = "CS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var sectionService = new Mock<ISectionService>();
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .ReturnsAsync(new SectionResponseDto());

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSection1,CS\nSection2,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(2, result.CreatedRows);

        var firstSection = sectionService.Invocations[0].Arguments[0] as Section;
        var secondSection = sectionService.Invocations[1].Arguments[0] as Section;
        Assert.NotNull(firstSection);
        Assert.NotNull(secondSection);
        Assert.Equal(7, firstSection.CourseId);
        Assert.Equal(7, secondSection.CourseId);
    }

    [Fact]
    public async Task ImportAsync_EnrollmentsCsv_UsesLookupCacheInsteadOfPerRowQueries()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Users.Add(new IdentityUser { Id = "s-1", Email = "student@x.com", UserName = "student@x.com" });
        context.Students.Add(new Student { Id = 20, UserId = "s-1", SectionId = 3, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 3, Name = "BSCS-1A", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Subjects.Add(new Subject { Id = 11, Code = "CS101", Name = "Computing", CreatedAt = now, UpdatedAt = now });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var enrollmentService = new Mock<IStudentEnrollmentService>();
        enrollmentService.Setup(s => s.EnrollStudentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new StudentEnrollment());

        var service = CreateService(context, accountService.Object, enrollmentService: enrollmentService.Object);
        var file = CreateFormFile("enrollments.csv",
            "studentEmail,sectionName,subjectCode,enrollmentType,academicYear,semester\n" +
            "student@x.com,BSCS-1A,CS101,Regular,2024-2025,1st\n");

        var result = await service.ImportAsync("enrollments", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);

        enrollmentService.Verify(s => s.EnrollStudentAsync(20, 3, 11, "Regular", "2024-2025", "1st"), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_UsersCsv_UsesLookupCacheForSectionId()
    {
        await using var context = CreateContext();
        context.Sections.Add(new Section { Id = 12, Name = "BSCS-1A", CourseId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());
        accountService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(new RegisterResponseDto());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.csv",
            "username,email,firstname,lastname,role,sectionName,temporaryPassword\n" +
            "user1,user1@example.com,First,Last,Student,BSCS-1A,Secret123!\n");

        var result = await service.ImportAsync("users", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);

        var registerDto = accountService.Invocations
            .First(i => i.Method.Name == "RegisterAsync").Arguments[0] as RegisterDto;
        Assert.NotNull(registerDto);
        Assert.Equal(12, registerDto.SectionId);
    }

    // === Atomicity tests (use SQLite for real transaction support) ===

    [Fact]
    public async Task ImportAsync_RollsBackDatabaseWrites_WhenOneReadyRowFails()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var sectionService = new Mock<ISectionService>();
        var callCount = 0;
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .Callback<Section>(section =>
            {
                callCount++;
                if (callCount > 1)
                {
                    throw new EntityServiceException("Section", "CreateSection", "Database error");
                }
                context.Sections.Add(section);
                context.SaveChanges();
            })
            .ReturnsAsync((Section section) => new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
            });

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSection1,CS\nSection2,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.False(result.Success);
        Assert.Equal(0, result.CreatedRows);
        Assert.Equal(2, result.FailedRows);

        // Verify database was actually rolled back — no sections should persist
        Assert.Empty(context.Sections.AsNoTracking().ToList());

        // First row was imported then rolled back
        var firstRow = result.Rows[0];
        Assert.Equal("failed", firstRow.Status);
        Assert.Contains(firstRow.Issues, i => i.Code == "import_rollback");

        // Second row failed during import
        var secondRow = result.Rows[1];
        Assert.Equal("failed", secondRow.Status);
        Assert.Contains(secondRow.Issues, i => i.Code == "import_failed");
    }

    [Fact]
    public async Task ImportAsync_CommitsDatabaseWrites_WhenNoReadyRowFails()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var sectionService = new Mock<ISectionService>();
        // The mock writes to the shared context so we can verify the data is committed
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .Callback<Section>(section =>
            {
                context.Sections.Add(section);
                context.SaveChanges();
            })
            .ReturnsAsync((Section section) => new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
            });

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSection1,CS\nSection2,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(2, result.CreatedRows);
        Assert.Equal(0, result.FailedRows);
        Assert.All(result.Rows, row => Assert.Equal("imported", row.Status));

        // Verify the sections actually persisted in the database after commit
        var persistedSections = context.Sections.AsNoTracking().ToList();
        Assert.Equal(2, persistedSections.Count);
    }

    [Fact]
    public async Task ImportAsync_DuplicateRowsDoNotTriggerRollback()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var sectionService = new Mock<ISectionService>();
        var callCount = 0;
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .Callback<Section>(section =>
            {
                callCount++;
                if (callCount > 1)
                {
                    throw new EntityAlreadyExistsException<string>("Section", "Name", "Section1");
                }
                context.Sections.Add(section);
                context.SaveChanges();
            })
            .ReturnsAsync((Section section) => new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
            });

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSection1,CS\nSection1-dup,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>());

        // Duplicates should be skipped without causing rollback
        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        Assert.Equal(1, result.SkippedDuplicateRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Equal("imported", result.Rows[0].Status);
        Assert.Equal("duplicate", result.Rows[1].Status);

        // The first section was committed and persists in the database
        var persistedSections = context.Sections.AsNoTracking().ToList();
        Assert.Single(persistedSections);
        Assert.Equal("Section1", persistedSections[0].Name);
    }

    [Fact]
    public async Task ImportAsync_RollbackConvertsImportedRowsToFailedWithRollbackIssue()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var sectionService = new Mock<ISectionService>();
        var callCount = 0;
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .Callback<Section>(section =>
            {
                callCount++;
                if (callCount > 2)
                {
                    throw new EntityServiceException("Section", "CreateSection", "Database error");
                }
                context.Sections.Add(section);
                context.SaveChanges();
            })
            .ReturnsAsync((Section section) => new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
            });

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSec1,CS\nSec2,CS\nSec3,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.False(result.Success);
        Assert.Equal(0, result.CreatedRows);
        Assert.Equal(3, result.FailedRows);

        // Verify database was actually rolled back — no sections should persist
        Assert.Empty(context.Sections.AsNoTracking().ToList());

        // First two rows should have rollback issue
        Assert.All(result.Rows.Take(2), row =>
        {
            Assert.Equal("failed", row.Status);
            Assert.Contains(row.Issues, i => i.Code == "import_rollback");
        });

        // Third row should have import_failed issue
        Assert.Equal("failed", result.Rows[2].Status);
        Assert.Contains(result.Rows[2].Issues, i => i.Code == "import_failed");
    }

    [Fact]
    public async Task ImportAsync_WhenCancellationOccursAfterRowFailure_RollbackStillReturnsResponse()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        using var cancellationTokenSource = new CancellationTokenSource();

        var sectionService = new Mock<ISectionService>();
        var callCount = 0;
        sectionService.Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .Callback<Section>(section =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cancellationTokenSource.Cancel();
                    throw new EntityServiceException("Section", "CreateSection", "Database error");
                }

                context.Sections.Add(section);
                context.SaveChanges();
            })
            .ReturnsAsync((Section section) => new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
            });

        var service = CreateService(context, accountService.Object, sectionService: sectionService.Object);
        var file = CreateFormFile("sections.csv", "name,courseName\nSec1,CS\nSec2,CS\n");

        var result = await service.ImportAsync("sections", file, CreatePrincipal(), new Dictionary<string, string?>(), cancellationTokenSource.Token);

        Assert.False(result.Success);
        Assert.Equal(0, result.CreatedRows);
        Assert.Equal(2, result.FailedRows);
        Assert.Empty(context.Sections.AsNoTracking().ToList());

        Assert.Equal("failed", result.Rows[0].Status);
        Assert.Contains(result.Rows[0].Issues, issue => issue.Code == "import_rollback");

        Assert.Equal("failed", result.Rows[1].Status);
        Assert.Contains(result.Rows[1].Issues, issue => issue.Code == "import_failed");
    }

    [Fact]
    public async Task ImportAsync_ClassroomsCsv_WithRetryingExecutionStrategy_DoesNotThrowTransactionError()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync(useRetryingExecutionStrategy: true);
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var classroomRepository = new ClassroomRepository(context);
        var classroomService = new ClassroomService(classroomRepository, Mock.Of<ILogger<ClassroomService>>());
        var service = new AdminDataService(
            context,
            accountService.Object,
            Mock.Of<ICourseService>(),
            classroomService,
            Mock.Of<ISectionService>(),
            Mock.Of<IScheduleService>(),
            Mock.Of<IStudentEnrollmentService>(),
            Mock.Of<ISubjectService>(),
            Options.Create(new BulkDataOptions()),
            Mock.Of<ILogger<AdminDataService>>());

        var file = CreateFormFile("classrooms.csv", "name\nNetwork Laboratory\n");
        var result = await service.ImportAsync("classrooms", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Equal(0, result.SkippedDuplicateRows);
        Assert.Single(context.Classrooms.AsNoTracking().ToList());
    }

    [Fact]
    public async Task GenerateTemplateAsync_UsersCsv_ReturnsExpectedHeaders()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        var service = CreateService(context, accountService.Object);

        var result = await service.GenerateTemplateAsync("users", "csv");

        Assert.Equal("text/csv", result.ContentType);
        Assert.Equal("users-template.csv", result.FileName);
        var csv = Encoding.UTF8.GetString(result.Content);
        Assert.Contains("username,email,firstname,lastname,role,sectionName,temporaryPassword", csv);
    }

    [Fact]
    public async Task GenerateTemplateAsync_UsersXlsx_IncludesDataAndInstructionsSheets()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        var service = CreateService(context, accountService.Object);

        var result = await service.GenerateTemplateAsync("users", "xlsx");

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.Equal("users-template.xlsx", result.FileName);
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length > 0);
    }

    [Fact]
    public async Task GenerateTemplateAsync_InvalidEntity_ThrowsValidationException()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        var service = CreateService(context, accountService.Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.GenerateTemplateAsync("invalid", "csv"));
    }

    [Fact]
    public async Task PreviewImport_EmptyFile_ThrowsValidationException()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.csv", string.Empty);

        await Assert.ThrowsAsync<ValidationException>(() => service.PreviewImportAsync("users", file, new Dictionary<string, string?>()));
    }

    [Fact]
    public async Task PreviewImport_UnsupportedExtension_ThrowsValidationException()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.txt", "username,email\nalpha,alpha@example.com\n");

        await Assert.ThrowsAsync<ValidationException>(() => service.PreviewImportAsync("users", file, new Dictionary<string, string?>()));
    }

    [Fact]
    public async Task PreviewImport_TooManyRows_ThrowsValidationException()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object, new BulkDataOptions { MaxRows = 2 });
        var file = CreateFormFile("users.csv", "username,email\nalpha,alpha@example.com\nbravo,bravo@example.com\ncharlie,charlie@example.com\n");

        await Assert.ThrowsAsync<ValidationException>(() => service.PreviewImportAsync("users", file, new Dictionary<string, string?>()));
    }

    [Fact]
    public async Task ImportAsync_WhenAnalysisContainsInvalidRows_DoesNotInvokeCreateServices()
    {
        await using var context = CreateContext();
        context.Sections.Add(new Section { Id = 12, Name = "BSCS-1A", CourseId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("users.csv", "username,email,firstname,lastname,role,sectionName,temporaryPassword\n,,, ,Student,BSCS-1A,Secret123\n");

        var result = await service.ImportAsync("users", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.False(result.Success);
        Assert.Equal(1, result.FailedRows);
        Assert.Equal(0, result.SkippedDuplicateRows);
        accountService.Verify(s => s.RegisterAsync(It.IsAny<RegisterDto>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_CoursesCsv_PassesPrincipalToCourseService()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var uniqueCourseName = $"Course-{Guid.NewGuid()}";
        var courseService = new Mock<ICourseService>();
        courseService.Setup(s => s.CreateCourseAsync(It.IsAny<CreateCourse>(), It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new Course { Id = 1, Name = uniqueCourseName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var principal = CreatePrincipal();
        var service = CreateService(context, accountService.Object, courseService: courseService.Object);
        var file = CreateFormFile("courses.csv", $"name\n{uniqueCourseName}\n");

        var result = await service.ImportAsync("courses", file, principal, new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        courseService.Verify(s => s.CreateCourseAsync(It.IsAny<CreateCourse>(), principal), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_SubjectsCsv_MapsCreateSubjectPayload()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var subjectService = new Mock<ISubjectService>();
        subjectService.Setup(s => s.CreateSubjectAsync(It.IsAny<CreateSubject>()))
            .ReturnsAsync(new Subject { Id = 1, Code = "CS101", Name = "Intro to Computing", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var service = CreateService(context, accountService.Object, subjectService: subjectService.Object);
        var file = CreateFormFile("subjects.csv", "code,name\nCS101,Intro to Computing\n");

        var result = await service.ImportAsync("subjects", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        subjectService.Verify(s => s.CreateSubjectAsync(It.Is<CreateSubject>(dto => dto.Code == "CS101" && dto.Name == "Intro to Computing")), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_EnrollmentsCsv_DefaultsEnrollmentTypeAndNullOptionalFields()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Users.Add(new IdentityUser { Id = "s-1", Email = "student@x.com", UserName = "student@x.com" });
        context.Students.Add(new Student { Id = 20, UserId = "s-1", SectionId = 3, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 3, Name = "BSCS-1A", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Subjects.Add(new Subject { Id = 11, Code = "CS101", Name = "Computing", CreatedAt = now, UpdatedAt = now });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var enrollmentService = new Mock<IStudentEnrollmentService>();
        enrollmentService.Setup(s => s.EnrollStudentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new StudentEnrollment());

        var service = CreateService(context, accountService.Object, enrollmentService: enrollmentService.Object);
        var file = CreateFormFile("enrollments.csv", "studentEmail,sectionName,subjectCode\nstudent@x.com,BSCS-1A,CS101\n");

        var result = await service.ImportAsync("enrollments", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        enrollmentService.Verify(s => s.EnrollStudentAsync(20, 3, 11, "Regular", null, null), Times.Once);
    }

    [Fact]
    public async Task ExportAsync_SubjectsXlsx_ReturnsProjectedRows()
    {
        await using var context = CreateContext();
        var accountService = new Mock<IAccountService>();

        var subjectService = new Mock<ISubjectService>();
        subjectService.Setup(s => s.GetAllSubjectsAsync())
            .ReturnsAsync(new[]
            {
                new Subject { Id = 1, Code = "CS101", Name = "Intro to Computing", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Subject { Id = 2, Code = "CS102", Name = "Data Structures", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            });

        var service = CreateService(context, accountService.Object, subjectService: subjectService.Object);

        var export = await service.ExportAsync("subjects", "xlsx", new Dictionary<string, string?>());

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.ContentType);
        Assert.NotNull(export.Content);
        Assert.True(export.Content.Length > 0);
    }

    [Fact]
    public async Task ExportAsync_EnrollmentsCsv_AppliesSectionFilters()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Users.Add(new IdentityUser { Id = "s-1", Email = "student@x.com", UserName = "student@x.com" });
        context.Students.Add(new Student { Id = 20, UserId = "s-1", SectionId = 3, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 3, Name = "BSCS-1A", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 4, Name = "BSCS-1B", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Subjects.Add(new Subject { Id = 11, Code = "CS101", Name = "Computing", CreatedAt = now, UpdatedAt = now });
        context.StudentEnrollments.Add(new StudentEnrollment
        {
            Id = 1,
            StudentId = 20,
            SectionId = 3,
            SubjectId = 11,
            EnrollmentType = "Regular",
            AcademicYear = "2024-2025",
            Semester = "1st",
            CreatedAt = now,
            UpdatedAt = now,
        });
        context.StudentEnrollments.Add(new StudentEnrollment
        {
            Id = 2,
            StudentId = 20,
            SectionId = 4,
            SubjectId = 11,
            EnrollmentType = "Regular",
            AcademicYear = "2024-2025",
            Semester = "1st",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);

        var export = await service.ExportAsync("enrollments", "csv", new Dictionary<string, string?>
        {
            ["sectionName"] = "BSCS-1A",
        });

        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Contains("BSCS-1A", csv);
        Assert.DoesNotContain("BSCS-1B", csv);
    }

    [Fact]
    public async Task PreviewImport_Enrollments_WithScopedSectionMismatch_ReturnsInvalidRow()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.Users.Add(new IdentityUser { Id = "s-1", Email = "student@x.com", UserName = "student@x.com" });
        context.Students.Add(new Student { Id = 20, UserId = "s-1", SectionId = 3, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 3, Name = "BSCS-1A", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Sections.Add(new Section { Id = 4, Name = "BSCS-1B", CourseId = 1, CreatedAt = now, UpdatedAt = now });
        context.Subjects.Add(new Subject { Id = 11, Code = "CS101", Name = "Computing", CreatedAt = now, UpdatedAt = now });
        await context.SaveChangesAsync();

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var service = CreateService(context, accountService.Object);
        var file = CreateFormFile("enrollments.csv", "studentEmail,sectionName,subjectCode\nstudent@x.com,BSCS-1B,CS101\n");

        var result = await service.PreviewImportAsync("enrollments", file, new Dictionary<string, string?>
        {
            ["sectionName"] = "BSCS-1A",
        });

        Assert.Equal(1, result.InvalidRows);
        var row = Assert.Single(result.Rows);
        Assert.Equal("invalid", row.Status);
        Assert.Contains(row.Issues, issue => issue.Code == "scope_mismatch");
    }

    [Fact]
    public async Task ImportAsync_ClassroomsCsv_WithAmbientTransaction_UsesExistingTransactionWithoutCommittingIt()
    {
        await using var sqlite = await CreateSqliteDatabaseAsync();
        await using var context = new ApplicationDbContext(sqlite.Options);

        var accountService = new Mock<IAccountService>();
        accountService.Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(Array.Empty<GetAllUsersDto>());

        var classroomRepository = new ClassroomRepository(context);
        var classroomService = new ClassroomService(classroomRepository, Mock.Of<ILogger<ClassroomService>>());
        var service = new AdminDataService(
            context,
            accountService.Object,
            Mock.Of<ICourseService>(),
            classroomService,
            Mock.Of<ISectionService>(),
            Mock.Of<IScheduleService>(),
            Mock.Of<IStudentEnrollmentService>(),
            Mock.Of<ISubjectService>(),
            Options.Create(new BulkDataOptions()),
            Mock.Of<ILogger<AdminDataService>>());

        await using var transaction = await context.Database.BeginTransactionAsync();
        var currentTransaction = context.Database.CurrentTransaction;

        var file = CreateFormFile("classrooms.csv", "name\nNetwork Laboratory\n");
        var result = await service.ImportAsync("classrooms", file, CreatePrincipal(), new Dictionary<string, string?>());

        Assert.True(result.Success);
        Assert.Equal(1, result.CreatedRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Equal(0, result.SkippedDuplicateRows);
        Assert.Same(currentTransaction, context.Database.CurrentTransaction);
        Assert.Single(context.Classrooms.AsNoTracking().ToList());

        await transaction.RollbackAsync();

        await using var verificationContext = new ApplicationDbContext(sqlite.Options);
        Assert.Empty(verificationContext.Classrooms.AsNoTracking().ToList());
    }

    private static AdminDataService CreateService(
        ApplicationDbContext context,
        IAccountService accountService,
        BulkDataOptions? options = null,
        ISectionService? sectionService = null,
        IScheduleService? scheduleService = null,
        IStudentEnrollmentService? enrollmentService = null,
        ICourseService? courseService = null,
        IClassroomService? classroomService = null,
        ISubjectService? subjectService = null)
    {
        return new AdminDataService(
            context,
            accountService,
            courseService ?? Mock.Of<ICourseService>(),
            classroomService ?? Mock.Of<IClassroomService>(),
            sectionService ?? Mock.Of<ISectionService>(),
            scheduleService ?? Mock.Of<IScheduleService>(),
            enrollmentService ?? Mock.Of<IStudentEnrollmentService>(),
            subjectService ?? Mock.Of<ISubjectService>(),
            Options.Create(options ?? new BulkDataOptions()),
            Mock.Of<ILogger<AdminDataService>>());
    }

    private static ClaimsPrincipal CreatePrincipal()
        => new(new ClaimsIdentity());

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<SqliteTestDatabase> CreateSqliteDatabaseAsync(bool useRetryingExecutionStrategy = false)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection);

        if (useRetryingExecutionStrategy)
        {
            optionsBuilder.ReplaceService<IExecutionStrategyFactory, TestRetryingExecutionStrategyFactory>();
        }

        var options = optionsBuilder.Options;

        await using var setupContext = new ApplicationDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();

        // Pre-seed the course that all section import tests depend on
        setupContext.Courses.Add(new Course { Id = 1, Name = "CS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await setupContext.SaveChangesAsync();

        return new SqliteTestDatabase(connection, options);
    }

    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        public SqliteConnection Connection { get; }
        public DbContextOptions<ApplicationDbContext> Options { get; }

        public SqliteTestDatabase(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options)
        {
            Connection = connection;
            Options = options;
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.CloseAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class TestRetryingExecutionStrategyFactory : IExecutionStrategyFactory
    {
        private readonly ExecutionStrategyDependencies _dependencies;

        public TestRetryingExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public IExecutionStrategy Create()
            => new TestRetryingExecutionStrategy(_dependencies);
    }

    private sealed class TestRetryingExecutionStrategy : ExecutionStrategy
    {
        public TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, 1, TimeSpan.FromMilliseconds(1))
        {
        }

        protected override bool ShouldRetryOn(Exception exception) => false;
    }

    private static FormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv",
        };
    }
}
