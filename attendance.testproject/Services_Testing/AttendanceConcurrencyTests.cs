using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Options;
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
    private readonly ConfiguredTimeZoneProvider _timeZoneProvider;

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

    [Fact]
    public async Task CreateAttendanceAsync_ConcurrentDuplicate_ReturnsExistingAttendanceForEquivalentRetry()
    {
        // Arrange
        var studentUuid = Guid.NewGuid();
        var sessionUuid = Guid.NewGuid();
        var request = new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = sessionUuid,
            Status = "Late",
            Notes = "Retry with changed payload fields",
            CheckInTime = DateTime.UtcNow.AddMinutes(5)
        };

        var user = CreateInstructorUser("instructor-1");
        var sectionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        // Setup mocks for validation
        _mockUserManager.Setup(um => um.FindByIdAsync("instructor-1"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-1" });

        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync("instructor-1"))
            .ReturnsAsync(new Instructor { Id = Guid.NewGuid() });

        _mockStudentRepository.Setup(r => r.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync(new Student { Id = studentUuid, SectionId = sectionId });

        _mockSessionRepository.Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session
            {
                Id = sessionUuid,
                Schedule = new Schedules
                {
                    InstructorId = Guid.NewGuid(),
                    SectionId = sectionId,
                    SubjectId = subjectId
                }
            });

        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(Guid.NewGuid()))
            .ReturnsAsync(new Session
            {
                Id = Guid.NewGuid(),
                Schedule = new Schedules
                {
                    InstructorId = Guid.NewGuid(),
                    SectionId = Guid.NewGuid(),
                    SubjectId = Guid.NewGuid()
                }
            });

        _mockStudentRepository.Setup(r => r.GetStudentByIdAsync(Guid.NewGuid()))
            .ReturnsAsync(new Student { Id = Guid.NewGuid(), SectionId = Guid.NewGuid() });

        _mockStudentEnrollmentRepository.Setup(r => r.GetStudentEnrollmentsAsync(studentUuid))
            .ReturnsAsync(new List<StudentEnrollment>
            {
                new StudentEnrollment { StudentId = studentUuid, SectionId = sectionId, SubjectId = subjectId }
            });

        var dbUpdateException = new DbUpdateException("Error", new Exception("UNIQUE constraint failed: IX_AttendanceRecords_StudentId_SessionId"));
        var existingRecord = CreateExistingAttendanceRecord(studentId: studentUuid, sessionId: sessionUuid);

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync(new AttendanceRecord { Id = Guid.NewGuid() });

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        _mockAttendanceRepository.Setup(r => r.GetBySessionAndStudentAsync(sessionUuid, studentUuid))
            .ReturnsAsync(existingRecord);

        // Act
        var result = await _attendanceService.CreateAttendanceAsync(request, user);

        // Assert
        Assert.Equal(existingRecord.Id, result.Id);
        Assert.Equal(existingRecord.Student.Id, result.StudentId);
        Assert.Equal(existingRecord.Session.Id, result.SessionId);
        Assert.Equal(existingRecord.Status, result.Status);
        Assert.Equal(existingRecord.Notes, result.Notes);
        Assert.True(result.IsManualEntry);
        Assert.NotEqual(Guid.Empty, existingRecord.Id);
        Assert.NotEqual(Guid.Empty, existingRecord.Session.Id);

        // Verify warning log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Duplicate attendance - returning existing record")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockAttendanceRepository.Verify(r => r.GetBySessionAndStudentAsync(sessionUuid, studentUuid), Times.Once);
    }

    [Fact]
    public async Task CreateAttendanceFromQrScanAsync_ConcurrentDuplicate_ThrowsInvalidOperationException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var qrCodeId = Guid.NewGuid();
        DateTime checkInTime = DateTime.UtcNow;

        // Setup mocks
        var qrSession = new Session
        {
            Id = Guid.NewGuid(),
            Schedule = new Schedules
            {
                Id = Guid.NewGuid(),
                TimeIn = TimeOnly.FromDateTime(DateTime.UtcNow)
            }
        };

        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(qrSession);

        // Simulate race condition: HasAttendanceRecordAsync returns false (check passed)
        // But CreateAsync/SaveChangesAsync throws DbUpdateException (DB constraint hit)
        _mockAttendanceRepository.Setup(r => r.HasAttendanceRecordAsync(studentId, sessionId))
            .ReturnsAsync(false);

        var dbUpdateException = new DbUpdateException("Error", new Exception("UNIQUE constraint failed: IX_AttendanceRecords_StudentId_SessionId"));

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync(new AttendanceRecord { Id = Guid.NewGuid() });

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _attendanceService.CreateAttendanceFromQrScanAsync(studentId, sessionId, qrCodeId, checkInTime)
        );

        Assert.Contains("duplicate - Attendance record already exists", exception.Message);
        Assert.NotEqual(Guid.Empty, qrSession.Id);
        Assert.NotEqual(Guid.Empty, qrSession.Schedule.Id);

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

    [Fact]
    public async Task CreateAttendanceFromQrScanAsync_UsesSessionDate_WhenActualStartTimeIsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var qrCodeId = Guid.NewGuid();
        var sessionDate = new DateTime(2026, 1, 5);
        var checkInTime = sessionDate.AddHours(8).AddMinutes(20);
        var createdRecord = new AttendanceRecord();

        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(new Session
            {
                Id = sessionId,
                SessionDate = sessionDate,
                Schedule = new Schedules
                {
                    TimeIn = new TimeOnly(8, 0)
                }
            });

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .Callback<AttendanceRecord>(record =>
            {
                createdRecord = new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = record.StudentId,
                    SessionId = record.SessionId,
                    QrCodeId = record.QrCodeId,
                    CheckInTime = record.CheckInTime,
                    Status = record.Status,
                    IsManualEntry = record.IsManualEntry
                };
            })
            .ReturnsAsync(() => createdRecord);

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockAttendanceRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() =>
            {
                createdRecord.Student = new Student
                {
                    Id = studentId,
                    Firstname = "John",
                    Lastname = "Doe"
                };
                createdRecord.Session = new Session
                {
                    Id = sessionId,
                    Schedule = new Schedules
                    {
                        Classroom = new Classroom { Name = "Room 301" },
                        Instructor = new Instructor { Firstname = "Ada", Lastname = "Lovelace" },
                        Subject = new Subject { Name = "Software Engineering" },
                        Section = new Section { Name = "BSCS 3A" }
                    }
                };
                return createdRecord;
            });

        // Act
        var result = await _attendanceService.CreateAttendanceFromQrScanAsync(studentId, sessionId, qrCodeId, checkInTime);

        // Assert
        Assert.Equal("Late", createdRecord.Status);
        Assert.Equal("Late", result.Status);
    }

    [Fact]
    public async Task CreateAttendanceAsync_WithoutCheckInTime_DefaultsToLocalTime()
    {
        // Arrange
        var studentUuid = Guid.NewGuid();
        var sessionUuid = Guid.NewGuid();
        var request = new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = sessionUuid,
            Status = "Present",
            Notes = "Manual check-in without explicit timestamp"
        };

        var user = CreateInstructorUser("instructor-1");
        DateTime capturedCheckInTime = default;
        var createdId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        _mockUserManager.Setup(um => um.FindByIdAsync("instructor-1"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-1" });

        _mockStudentRepository.Setup(r => r.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync(new Student { Id = studentUuid, SectionId = sectionId });

        _mockSessionRepository.Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session
            {
                Id = sessionUuid,
                Schedule = new Schedules
                {
                    InstructorId = Guid.NewGuid(),
                    SectionId = sectionId,
                    SubjectId = subjectId
                }
            });

        _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(sessionUuid))
            .ReturnsAsync(new Session
            {
                Id = sessionUuid,
                Schedule = new Schedules
                {
                    InstructorId = Guid.NewGuid(),
                    SectionId = sectionId,
                    SubjectId = subjectId
                }
            });

        _mockStudentRepository.Setup(r => r.GetStudentByIdAsync(studentUuid))
            .ReturnsAsync(new Student { Id = studentUuid, SectionId = sectionId });

        _mockStudentEnrollmentRepository.Setup(r => r.GetStudentEnrollmentsAsync(studentUuid))
            .ReturnsAsync(new List<StudentEnrollment>
            {
                new StudentEnrollment { StudentId = studentUuid, SectionId = sectionId, SubjectId = subjectId }
            });

        _mockAttendanceRepository.Setup(r => r.CreateAsync(It.IsAny<AttendanceRecord>()))
            .Callback<AttendanceRecord>(record => capturedCheckInTime = record.CheckInTime)
            .ReturnsAsync(new AttendanceRecord { Id = createdId });

        _mockAttendanceRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockAttendanceRepository.Setup(r => r.GetByIdAsync(createdId))
            .ReturnsAsync(() =>
            {
                var record = CreateExistingAttendanceRecord(studentUuid, sessionUuid);
                record.Id = createdId;
                record.CheckInTime = capturedCheckInTime;
                record.Status = request.Status;
                record.Notes = request.Notes;
                record.EnteredBy = "instructor-1";
                return record;
            });

        // Act
        var result = await _attendanceService.CreateAttendanceAsync(request, user);

        // Assert
        // CheckInTime should be set (not default)
        Assert.NotEqual(default, capturedCheckInTime);
        Assert.NotEqual(default, result.CheckInTime);
        // Time should come from TimeProvider (configured timezone)
        Assert.Equal(capturedCheckInTime, result.CheckInTime);
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

    private static AttendanceRecord CreateExistingAttendanceRecord(Guid studentId, Guid sessionId)
    {
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SessionId = sessionId,
            CheckInTime = DateTime.UtcNow.AddMinutes(-10),
            Status = "Present",
            Notes = "Original attendance record",
            IsManualEntry = true,
            EnteredBy = "instructor-1",
            Student = new Student
            {
                Id = studentId,
                Firstname = "Sam",
                Lastname = "Student"
            },
            Session = new Session
            {
                Id = Guid.NewGuid(),
                SessionDate = DateTime.UtcNow.Date,
                ScheduleId = Guid.NewGuid(),
                ActualRoom = new Classroom
                {
                    Id = Guid.NewGuid(),
                    Name = "Integration Room 1"
                },
                Schedule = new Schedules
                {
                    Id = Guid.NewGuid(),
                    Subject = new Subject
                    {
                        Id = Guid.NewGuid(),
                        Name = "Integration Testing"
                    },
                    Section = new Section
                    {
                        Id = Guid.NewGuid(),
                        Name = "INT-SEC-A"
                    },
                    Classroom = new Classroom
                    {
                        Id = Guid.NewGuid(),
                        Name = "Integration Room 1"
                    },
                    Instructor = new Instructor
                    {
                        Id = Guid.NewGuid(),
                        Firstname = "Ivy",
                        Lastname = "Instructor"
                    }
                }
            }
        };
    }
}
