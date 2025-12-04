using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Concurrency and race condition tests for AttendanceService
/// </summary>
public class AttendanceConcurrencyTests
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

    public AttendanceConcurrencyTests()
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

        _attendanceService = new AttendanceService(
            _mockAttendanceRepository.Object,
            _mockStudentRepository.Object,
            _mockInstructorRepository.Object,
            _mockSessionRepository.Object,
            _mockStudentEnrollmentRepository.Object,
            _userContextService,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateAttendanceAsync_ConcurrentDuplicate_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateAttendanceRequest
        {
            StudentId = 1,
            SessionId = 1,
            Status = "Present"
        };

        var user = CreateInstructorUser("instructor-1");

        // Setup mocks for validation
        _mockUserManager.Setup(um => um.FindByIdAsync("instructor-1"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-1" });

        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync("instructor-1"))
            .ReturnsAsync(new Instructor { Id = 10 });

        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(1))
            .ReturnsAsync(new Session
            {
                Id = 1,
                Schedule = new Schedules { InstructorId = 10, SectionId = 100 }
            });

        _mockStudentRepository.Setup(r => r.GetStudentByIdAsync(1))
            .ReturnsAsync(new Student { Id = 1, SectionId = 100 });

        _mockStudentEnrollmentRepository.Setup(r => r.GetStudentEnrollmentsAsync(1))
            .ReturnsAsync(new List<StudentEnrollment>
            {
                new StudentEnrollment { StudentId = 1, SectionId = 100 }
            });

        // Simulate race condition: HasAttendanceRecordAsync returns false (check passed)
        // But CreateAsync/SaveChangesAsync throws DbUpdateException (DB constraint hit)
        _mockAttendanceRepository.Setup(r => r.HasAttendanceRecordAsync(1, 1))
            .ReturnsAsync(false);

        var dbUpdateException = new DbUpdateException("Error", new Exception("UNIQUE constraint failed: IX_AttendanceRecords_StudentId_SessionId"));

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync(new AttendanceRecord { Id = 100 });

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _attendanceService.CreateAttendanceAsync(request, user)
        );

        Assert.Contains("Attendance record already exists", exception.Message);

        // Verify warning log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Duplicate attendance - race condition detected")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAttendanceFromQrScanAsync_ConcurrentDuplicate_ThrowsInvalidOperationException()
    {
        // Arrange
        int studentId = 1;
        int sessionId = 1;
        int qrCodeId = 1;
        DateTime checkInTime = DateTime.UtcNow;

        // Setup mocks
        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(new Session
            {
                Id = sessionId,
                Schedule = new Schedules { TimeIn = TimeOnly.FromDateTime(DateTime.UtcNow) }
            });

        // Simulate race condition: HasAttendanceRecordAsync returns false (check passed)
        // But CreateAsync/SaveChangesAsync throws DbUpdateException (DB constraint hit)
        _mockAttendanceRepository.Setup(r => r.HasAttendanceRecordAsync(studentId, sessionId))
            .ReturnsAsync(false);

        var dbUpdateException = new DbUpdateException("Error", new Exception("UNIQUE constraint failed: IX_AttendanceRecords_StudentId_SessionId"));

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync(new AttendanceRecord { Id = 100 });

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _attendanceService.CreateAttendanceFromQrScanAsync(studentId, sessionId, qrCodeId, checkInTime)
        );

        Assert.Contains("duplicate - Attendance record already exists", exception.Message);

        // Verify warning log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Duplicate QR scan - race condition detected")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private ClaimsPrincipal CreateInstructorUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, RoleConstants.Instructor)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
