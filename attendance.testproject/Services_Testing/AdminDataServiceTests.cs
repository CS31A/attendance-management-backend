using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Options;
using attendance_monitoring.Services.AdminData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    private static AdminDataService CreateService(ApplicationDbContext context, IAccountService accountService, BulkDataOptions? options = null)
    {
        return new AdminDataService(
            context,
            accountService,
            Mock.Of<ICourseService>(),
            Mock.Of<IClassroomService>(),
            Mock.Of<ISectionService>(),
            Mock.Of<IScheduleService>(),
            Mock.Of<IStudentEnrollmentService>(),
            Mock.Of<ISubjectService>(),
            Options.Create(options ?? new BulkDataOptions()),
            Mock.Of<ILogger<AdminDataService>>());
    }

    private static ClaimsPrincipal CreatePrincipal()
        => new(new ClaimsIdentity());

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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
