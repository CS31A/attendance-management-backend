using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Authorization edge case tests for AttendanceService
/// Tests the fail-secure pattern for student authorization
/// </summary>
public class AttendanceAuthorizationTests
{
    private readonly Mock<IAttendanceRepository> _mockAttendanceRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IInstructorRepository> _mockInstructorRepository;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IStudentEnrollmentRepository> _mockStudentEnrollmentRepository;
    private readonly Mock<ILogger<AttendanceService>> _mockLogger;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly UserContextService _userContextService;
    private readonly AttendanceService _attendanceService;
    private readonly ConfiguredTimeZoneProvider _timeZoneProvider;

    public AttendanceAuthorizationTests()
    {
        _mockAttendanceRepository = new Mock<IAttendanceRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockStudentEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        _mockLogger = new Mock<ILogger<AttendanceService>>();

        // Mock UserManager for UserContextService
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<IdentityUser>>().Object,
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockContext = new Mock<ApplicationDbContext>(options);

        // Create real UserContextService with mocked UserManager and context
        _userContextService = new UserContextService(_mockUserManager.Object, mockContext.Object);

        // Use system local timezone for tests
        var systemTimeZoneId = TimeZoneInfo.Local.Id;
        var settings = new TimeZoneSettings { TimeZoneId = systemTimeZoneId };
        _timeZoneProvider = new ConfiguredTimeZoneProvider(settings);

        _attendanceService = new AttendanceService(
            _mockAttendanceRepository.Object,
            _mockStudentRepository.Object,
            _mockInstructorRepository.Object,
            _mockSessionRepository.Object,
            _mockStudentEnrollmentRepository.Object,
            _userContextService,
            _mockLogger.Object,
            _timeZoneProvider
        );
    }

    #region GetAllAttendanceAsync Authorization Tests

    [Fact]
    public async Task GetAllAttendanceAsync_StudentWithNullUserId_ThrowsUnauthorized()
    {
        // Arrange - Create user with only role claim (no NameIdentifier)
        var studentUser = CreateStudentUserWithoutUserId();
        var filter = new AttendanceFilterRequest();

        // Mock UserManager to return null for FindByNameAsync (fallback fails)
        _mockUserManager
            .Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attendanceService.GetAllAttendanceAsync(filter, studentUser)
        );

        Assert.Equal("Unable to verify user identity", exception.Message);
    }

    [Fact]
    public async Task GetAllAttendanceAsync_StudentWithoutProfile_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "user-123";
        var studentUser = CreateStudentUser(userId);
        var filter = new AttendanceFilterRequest();

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock StudentRepository to return null (edge case: user has Student role but no Student profile)
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByUserIdAsync(userId))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _attendanceService.GetAllAttendanceAsync(filter, studentUser)
        );

        Assert.Contains("Student profile not found", exception.Message);
    }

    [Fact]
    public async Task GetAllAttendanceAsync_ValidStudent_FiltersCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var studentId = 1;
        var studentUser = CreateStudentUser(userId);
        var filter = new AttendanceFilterRequest();

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock StudentRepository to return valid student
        var student = CreateTestStudent(studentId, userId);
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByUserIdAsync(userId))
            .ReturnsAsync(student);

        // Mock AttendanceRepository to return filtered results
        var attendanceRecords = new List<AttendanceRecord>
        {
            CreateTestAttendanceRecord(1, studentId, 1)
        };
        _mockAttendanceRepository
            .Setup(ar => ar.GetFilteredAsync(
                It.Is<int?>(id => id == studentId), // Verify studentId is set in filter
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((attendanceRecords, 1));

        // Act
        var result = await _attendanceService.GetAllAttendanceAsync(filter, studentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(student.Uuid, result.Items[0].StudentId);
    }

    #endregion

    #region GetAttendanceSummaryAsync Authorization Tests

    [Fact]
    public async Task GetAttendanceSummaryAsync_StudentWithNullUserId_ThrowsUnauthorized()
    {
        // Arrange - Create user with only role claim (no NameIdentifier)
        var studentUser = CreateStudentUserWithoutUserId();
        var filter = new AttendanceFilterRequest();

        // Mock UserManager to return null for FindByNameAsync (fallback fails)
        _mockUserManager
            .Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attendanceService.GetAttendanceSummaryAsync(filter, studentUser)
        );

        Assert.Equal("Unable to verify user identity", exception.Message);
    }

    [Fact]
    public async Task GetAttendanceSummaryAsync_StudentWithoutProfile_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "user-123";
        var studentUser = CreateStudentUser(userId);
        var filter = new AttendanceFilterRequest();

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock StudentRepository to return null
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByUserIdAsync(userId))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _attendanceService.GetAttendanceSummaryAsync(filter, studentUser)
        );

        Assert.Contains("Student profile not found", exception.Message);
    }

    [Fact]
    public async Task GetAttendanceSummaryAsync_ValidStudent_FiltersCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var studentId = 1;
        var studentUser = CreateStudentUser(userId);
        var filter = new AttendanceFilterRequest();

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock StudentRepository to return valid student
        var student = CreateTestStudent(studentId, userId);
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByUserIdAsync(userId))
            .ReturnsAsync(student);

        // Mock AttendanceRepository to return statistics
        _mockAttendanceRepository
            .Setup(ar => ar.GetStatisticsAsync(
                It.Is<int?>(id => id == studentId), // Verify studentId is set in filter
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync((10, 8, 1, 1, 0, 0L));

        // Act
        var result = await _attendanceService.GetAttendanceSummaryAsync(filter, studentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalSessions);
        Assert.Equal(8, result.TotalPresent);
    }

    #endregion

    #region GetStudentAttendanceHistoryAsync Authorization Tests

    [Fact]
    public async Task GetStudentAttendanceHistoryAsync_StudentWithNullUserId_ThrowsUnauthorized()
    {
        // Arrange - Create user with only role claim (no NameIdentifier)
        var studentId = 1;
        var studentUser = CreateStudentUserWithoutUserId();

        // Mock student exists
        var student = CreateTestStudent(studentId, "other-user-456");
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByIdAsync(studentId))
            .ReturnsAsync(student);

        // Mock UserManager to return null for FindByNameAsync (fallback fails)
        _mockUserManager
            .Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attendanceService.GetStudentAttendanceHistoryAsync(studentId, studentUser)
        );

        Assert.Equal("Unable to verify user identity", exception.Message);
    }

    [Fact]
    public async Task GetStudentAttendanceHistoryAsync_StudentViewingOtherStudent_ThrowsUnauthorized()
    {
        // Arrange
        var studentId = 1;
        var userId = "user-123";
        var otherUserId = "other-user-456";
        var studentUser = CreateStudentUser(userId);

        // Mock student exists with different userId
        var student = CreateTestStudent(studentId, otherUserId);
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByIdAsync(studentId))
            .ReturnsAsync(student);

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attendanceService.GetStudentAttendanceHistoryAsync(studentId, studentUser)
        );

        Assert.Equal("You can only view your own attendance history", exception.Message);
    }

    [Fact]
    public async Task GetStudentAttendanceHistoryAsync_StudentViewingOwnHistory_Succeeds()
    {
        // Arrange
        var studentId = 1;
        var userId = "user-123";
        var studentUser = CreateStudentUser(userId);

        // Mock student exists with matching userId
        var student = CreateTestStudent(studentId, userId);
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByIdAsync(studentId))
            .ReturnsAsync(student);

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock attendance records
        var attendanceRecords = new List<AttendanceRecord>
        {
            CreateTestAttendanceRecord(1, studentId, 1)
        };
        _mockAttendanceRepository
            .Setup(ar => ar.GetByStudentIdAsync(studentId))
            .ReturnsAsync(attendanceRecords);

        // Act
        var result = await _attendanceService.GetStudentAttendanceHistoryAsync(studentId, studentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(student.Uuid, result.StudentId);
        Assert.Single(result.AttendanceRecords);
    }

    #endregion

    #region GetAttendanceByIdAsync Authorization Tests

    [Fact]
    public async Task GetAttendanceById_StudentWithNullUserId_ThrowsUnauthorized()
    {
        // Arrange
        var attendanceId = 1;
        var studentUser = CreateStudentUserWithoutUserId();
        var attendanceRecord = CreateTestAttendanceRecord(attendanceId, 1, 1);

        _mockAttendanceRepository
            .Setup(ar => ar.GetByIdAsync(attendanceId))
            .ReturnsAsync(attendanceRecord);

        // Mock UserManager to return null for FindByNameAsync (fallback fails)
        _mockUserManager
            .Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attendanceService.GetAttendanceByIdAsync(attendanceId, studentUser)
        );

        Assert.Equal("Unable to verify user identity", exception.Message);
    }

    [Fact]
    public async Task GetAttendanceById_StudentWithoutProfile_ThrowsNotFoundException()
    {
        // Arrange
        var attendanceId = 1;
        var userId = "user-123";
        var studentUser = CreateStudentUser(userId);
        var attendanceRecord = CreateTestAttendanceRecord(attendanceId, 1, 1);

        _mockAttendanceRepository
            .Setup(ar => ar.GetByIdAsync(attendanceId))
            .ReturnsAsync(attendanceRecord);

        // Mock UserContextService to return valid userId
        var identityUser = new IdentityUser { Id = userId };
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Mock StudentRepository to return null
        _mockStudentRepository
            .Setup(sr => sr.GetStudentByUserIdAsync(userId))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _attendanceService.GetAttendanceByIdAsync(attendanceId, studentUser)
        );

        Assert.Contains("Student profile not found", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static ClaimsPrincipal CreateStudentUser(string userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Student"),
            new Claim(ClaimTypes.Name, "student@test.com")
        }, "TestAuthentication"));
    }

    private static ClaimsPrincipal CreateStudentUserWithoutUserId()
    {
        // Create user with only role claim - edge case where NameIdentifier is missing
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Student"),
            new Claim(ClaimTypes.Name, "student@test.com")
        }, "TestAuthentication"));
    }

    private static Student CreateTestStudent(int id, string userId)
    {
        return new Student
        {
            Id = id,
            UserId = userId,
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            IsDeleted = false,
            Section = CreateTestSection()
        };
    }

    private static Section CreateTestSection()
    {
        return new Section
        {
            Id = 1,
            Name = "CS-3A"
        };
    }

    private static AttendanceRecord CreateTestAttendanceRecord(int id, int studentId, int sessionId)
    {
        return new AttendanceRecord
        {
            Id = id,
            StudentId = studentId,
            SessionId = sessionId,
            CheckInTime = DateTime.UtcNow,
            Status = "Present",
            IsManualEntry = false,
            Student = CreateTestStudent(studentId, "user-123"),
            Session = CreateTestSession(sessionId)
        };
    }

    private static Session CreateTestSession(int id)
    {
        var instructor = new Instructor
        {
            Id = 1,
            UserId = "instructor-123",
            Firstname = "Jane",
            Lastname = "Smith",
            IsDeleted = false
        };

        var classroom = new Classroom
        {
            Id = 1,
            Name = "Room 101"
        };

        var subject = new Subject
        {
            Id = 1,
            Name = "Computer Science",
            Code = "CS101"
        };

        var section = CreateTestSection();

        var schedule = new Schedules
        {
            Id = 1,
            SubjectId = 1,
            SectionId = 1,
            InstructorId = 1,
            ClassroomId = 1,
            TimeIn = TimeOnly.Parse("08:00"),
            TimeOut = TimeOnly.Parse("09:30"),
            Subject = subject,
            Section = section,
            Instructor = instructor,
            Classroom = classroom
        };

        return new Session
        {
            Id = id,
            ScheduleId = 1,
            SessionDate = DateTime.Today,
            Status = SessionStatusConstants.Active,
            Schedule = schedule,
            ActualRoom = classroom
        };
    }

    #endregion
}
