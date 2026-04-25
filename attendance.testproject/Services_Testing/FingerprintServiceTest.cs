using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Services_Testing;

public class FingerprintServiceTest
{
    private readonly Mock<IFingerprintRepository> _mockFingerprintRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IScheduleRepository> _mockScheduleRepository;
    private readonly Mock<IStudentEnrollmentRepository> _mockStudentEnrollmentRepository;
    private readonly Mock<IAttendanceRepository> _mockAttendanceRepository;
    private readonly Mock<IAttendanceService> _mockAttendanceService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<FingerprintService>> _mockLogger;
    private readonly Mock<IDbContextTransaction> _mockTransaction;
    private readonly ConfiguredTimeZoneProvider _timeZoneProvider;
    private readonly ApplicationDbContext _context;
    private readonly UserContextService _userContextService;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public FingerprintServiceTest()
    {
        _mockFingerprintRepository = new Mock<IFingerprintRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockScheduleRepository = new Mock<IScheduleRepository>();
        _mockStudentEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        _mockAttendanceRepository = new Mock<IAttendanceRepository>();
        _mockAttendanceService = new Mock<IAttendanceService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<FingerprintService>>();
        _mockTransaction = new Mock<IDbContextTransaction>();

        _mockTransaction
            .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockTransaction
            .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _timeZoneProvider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        _mockAttendanceService
            .Setup(service => service.DetermineAttendanceStatus(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .Returns((DateTime checkInTime, DateTime sessionStartTime, int lateCutoffMinutes) =>
            {
                var timeDifference = checkInTime - sessionStartTime;
                if (timeDifference.TotalMinutes <= 0)
                {
                    return "Present";
                }

                return "Late";
            });

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(contextOptions);

        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        var mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<IdentityUser>>().Object,
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);

        _userContextService = new UserContextService(mockUserManager.Object, _context);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FingerprintDeviceAuth:Devices:esp32-attendance-01"] = "device-secret"
            })
            .Build();

        _dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public async Task StartEnrollmentSessionAsync_WithValidRequest_AssignsLowestAvailableSlot()
    {
        var service = CreateService();
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            IsDeleted = false
        };

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUuidAsync(student.Uuid))
            .ReturnsAsync(student);

        _mockFingerprintRepository
            .Setup(repository => repository.GetFingerprintByStudentIdIncludingDeletedAsync(student.Id))
            .ReturnsAsync((Fingerprint?)null);

        _mockFingerprintRepository
            .Setup(repository => repository.GetFingerprintsByDeviceIdAsync("esp32-attendance-01"))
            .ReturnsAsync(new[]
            {
                new Fingerprint { Id = 11, DeviceId = "esp32-attendance-01", SensorFingerprintId = 1, UserId = "user-a", TemplateData = "ciphertext" },
                new Fingerprint { Id = 12, DeviceId = "esp32-attendance-01", SensorFingerprintId = 3, UserId = "user-b", TemplateData = "ciphertext" }
            });

        var response = await service.StartEnrollmentSessionAsync(
            new StartFingerprintEnrollmentSessionRequest
            {
                StudentId = student.Uuid,
                DeviceId = "esp32-attendance-01"
            },
            CreatePrivilegedPrincipal());

        Assert.True(response.Success);
        Assert.Equal(2, response.AssignedSensorFingerprintId);
        Assert.Equal(student.Uuid, response.StudentId);
        Assert.Equal("John Doe", response.StudentName);
        Assert.NotEqual(Guid.Empty, response.Id);

        var persistedSession = await _context.FingerprintEnrollmentSessions.SingleAsync();
        Assert.Equal("Pending", persistedSession.Status);
        Assert.Equal(2, persistedSession.AssignedSensorFingerprintId);
        Assert.NotEqual(Guid.Empty, persistedSession.Uuid);
        Assert.NotEqual(Guid.Empty, persistedSession.EnrollmentSessionId);
        Assert.NotEqual(persistedSession.Uuid, persistedSession.EnrollmentSessionId);
    }

    [Fact]
    public async Task GetPendingEnrollmentSessionAsync_WithInvalidApiKey_ThrowsUnauthorized()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<EntityUnauthorizedException>(() =>
            service.GetPendingEnrollmentSessionAsync("esp32-attendance-01", "wrong-secret"));
    }

    [Fact]
    public async Task GetPendingEnrollmentSessionAsync_WithRegisteredDeviceAndMatchingApiKey_ReturnsPendingSession()
    {
        var service = CreateService();
        var now = DateTime.UtcNow;
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            IsDeleted = false
        };
        var enrollmentSession = new FingerprintEnrollmentSession
        {
            EnrollmentSessionId = Guid.NewGuid(),
            DeviceId = device.Id,
            StudentId = student.Id,
            RequestedByUserId = "admin-1",
            AssignedSensorFingerprintId = 9,
            Status = "Pending",
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.FingerprintDevices.Add(device);
        _context.Students.Add(student);
        _context.FingerprintEnrollmentSessions.Add(enrollmentSession);
        await _context.SaveChangesAsync();

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);

        var response = await service.GetPendingEnrollmentSessionAsync(device.DeviceIdentifier, "device-secret");

        Assert.NotNull(response);
        Assert.True(response!.Success);
        Assert.Equal(enrollmentSession.Uuid, response.Id);
        Assert.Equal(student.Uuid, response.StudentId);
        Assert.Equal("InProgress", response.Status);
    }

    [Fact]
    public async Task GetPendingEnrollmentSessionAsync_WithGlobalKeyForDifferentDevice_ThrowsUnauthorized()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FingerprintDeviceAuth:ApiKey"] = "legacy-global-secret",
                ["FingerprintDeviceAuth:Devices:esp32-attendance-01"] = "device-secret"
            })
            .Build();
        var service = CreateService(configuration);

        await Assert.ThrowsAsync<EntityUnauthorizedException>(() =>
            service.GetPendingEnrollmentSessionAsync("esp32-attendance-02", "legacy-global-secret"));

        Assert.Empty(_context.FingerprintDevices);
    }

    [Fact]
    public async Task CompleteEnrollmentSessionAsync_WithValidPayload_CreatesEncryptedFingerprintRecord()
    {
        var service = CreateService();
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            IsDeleted = false
        };

        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var enrollmentSession = new FingerprintEnrollmentSession
        {
            EnrollmentSessionId = Guid.NewGuid(),
            DeviceId = device.Id,
            StudentId = student.Id,
            RequestedByUserId = "admin-1",
            AssignedSensorFingerprintId = 9,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FingerprintDevices.Add(device);
        _context.FingerprintEnrollmentSessions.Add(enrollmentSession);
        await _context.SaveChangesAsync();

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);

        _mockFingerprintRepository
            .Setup(repository => repository.GetFingerprintByStudentIdIncludingDeletedAsync(student.Id))
            .ReturnsAsync((Fingerprint?)null);

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, enrollmentSession.AssignedSensorFingerprintId))
            .ReturnsAsync((Fingerprint?)null);

        Fingerprint? createdFingerprint = null;

        _mockFingerprintRepository
            .Setup(repository => repository.CreateFingerprintAsync(It.IsAny<Fingerprint>()))
            .ReturnsAsync((Fingerprint fingerprint) =>
            {
                createdFingerprint = fingerprint;
                fingerprint.Id = 99;
                fingerprint.Uuid = Guid.NewGuid();
                return fingerprint;
            });

        var rawTemplate = Convert.ToBase64String("raw-template-data"u8.ToArray());

        var response = await service.CompleteEnrollmentSessionAsync(
            new CompleteFingerprintEnrollmentRequest
            {
                Id = enrollmentSession.Uuid,
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = enrollmentSession.AssignedSensorFingerprintId,
                Success = true,
                BackupTemplateBase64 = rawTemplate
            },
            "device-secret");

        Assert.True(response.Success);
        Assert.Equal(createdFingerprint?.Uuid, response.Id);
        Assert.Equal(student.Uuid, response.StudentId);

        _mockFingerprintRepository.Verify(repository => repository.CreateFingerprintAsync(
            It.Is<Fingerprint>(fingerprint =>
                fingerprint.UserId == student.UserId &&
                fingerprint.DeviceId == device.DeviceIdentifier &&
                fingerprint.SensorFingerprintId == enrollmentSession.AssignedSensorFingerprintId &&
                fingerprint.TemplateData != rawTemplate &&
                !string.IsNullOrWhiteSpace(fingerprint.TemplateData))),
            Times.Once);

        var persistedSession = await _context.FingerprintEnrollmentSessions.SingleAsync();
        Assert.Equal("Completed", persistedSession.Status);
        Assert.NotNull(persistedSession.CompletedAt);
        Assert.NotNull(createdFingerprint);
        Assert.NotEqual(Guid.Empty, createdFingerprint!.Uuid);
        Assert.NotEqual(Guid.Empty, persistedSession.Uuid);
        Assert.NotEqual(Guid.Empty, persistedSession.EnrollmentSessionId);
        Assert.NotEqual(persistedSession.Uuid, persistedSession.EnrollmentSessionId);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_WithUnknownSlot_ReturnsNoMatchAndWritesEvent()
    {
        var service = CreateService();
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FingerprintDevices.Add(device);
        await _context.SaveChangesAsync();

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, 5))
            .ReturnsAsync((Fingerprint?)null);

        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 88
            },
            "device-secret");

        Assert.False(response.Success);
        Assert.Equal("No matching fingerprint found", response.Message);

        var scanEvent = await _context.FingerprintScanEvents.SingleAsync();
        Assert.Equal("NoMatch", scanEvent.Status);
        Assert.Equal(device.Id, scanEvent.DeviceId);
        Assert.Equal(0.3451m, scanEvent.MatchScore);
        Assert.NotEqual(Guid.Empty, scanEvent.Uuid);
        Assert.NotEqual(Guid.Empty, scanEvent.EventId);
        Assert.NotEqual(scanEvent.Uuid, scanEvent.EventId);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_WithInactiveEnrollmentForRequestedSession_ReturnsNotEnrolled()
    {
        var service = CreateService();
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            SectionId = 99,
            IsDeleted = false
        };
        var session = new Session
        {
            Id = 11,
            Uuid = Guid.NewGuid(),
            ScheduleId = 20,
            SessionDate = DateTime.Now.Date,
            Status = SessionStatusConstants.Active,
            ActualStartTime = DateTime.Now.AddMinutes(-5),
            Schedule = new Schedules
            {
                Id = 20,
                SectionId = 1,
                SubjectId = 2,
                Section = new Section { Id = 1, Name = "BSCS 3A" },
                Subject = new Subject { Id = 2, Name = "Software Engineering" },
                Instructor = new Instructor { Id = 3, Firstname = "Ada", Lastname = "Lovelace", UserId = "inst-1" }
            }
        };

        _context.FingerprintDevices.Add(device);
        await _context.SaveChangesAsync();

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, 5))
            .ReturnsAsync(new Fingerprint
            {
                Id = 42,
                UserId = student.UserId,
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                TemplateData = "ciphertext"
            });

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(session.Id))
            .ReturnsAsync(session);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(session.Uuid))
            .ReturnsAsync(session);
        _mockStudentEnrollmentRepository
            .Setup(repository => repository.GetByStudentSectionSubjectAsync(student.Id, session.Schedule.SectionId, session.Schedule.SubjectId))
            .ReturnsAsync(new StudentEnrollment
            {
                Id = 55,
                StudentId = student.Id,
                SectionId = session.Schedule.SectionId,
                SubjectId = session.Schedule.SubjectId,
                IsActive = false
            });
        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByStudentAndSessionAsync(student.Id, session.Id))
            .ReturnsAsync((AttendanceRecord?)null);

        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 90,
                SessionId = session.Uuid
            },
            "device-secret");

        Assert.False(response.Success);
        Assert.Equal("Student is not enrolled in this session", response.Message);

        _mockAttendanceRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_WithoutRequestedSession_UsesLocalTimeToFindActiveSession()
    {
        var service = CreateService();
        var expectedSectionIds = new[] { 1 };
        var expectedSubjectIds = new[] { 2 };
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var now = DateTime.Now;
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            SectionId = 1,
            IsDeleted = false
        };
        var schedule = new Schedules
        {
            Id = 20,
            SectionId = 1,
            SubjectId = 2,
            Section = new Section { Id = 1, Name = "BSCS 3A" },
            Subject = new Subject { Id = 2, Name = "Software Engineering" },
            Instructor = new Instructor { Id = 3, Firstname = "Ada", Lastname = "Lovelace", UserId = "inst-1" }
        };
        var session = new Session
        {
            Id = 11,
            Uuid = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            SessionDate = now.Date,
            Status = SessionStatusConstants.Active,
            ActualStartTime = now.AddMinutes(-5),
            ActualEndTime = now.AddMinutes(55),
            Schedule = schedule
        };

        _context.FingerprintDevices.Add(device);
        await _context.SaveChangesAsync();

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, 5))
            .ReturnsAsync(new Fingerprint
            {
                Id = 42,
                UserId = student.UserId,
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                TemplateData = "ciphertext"
            });

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);
        _mockStudentEnrollmentRepository
            .Setup(repository => repository.GetByStudentIdAsync(student.Id))
            .ReturnsAsync(new[]
            {
                new StudentEnrollment
                {
                    Id = 55,
                    StudentId = student.Id,
                    SectionId = schedule.SectionId,
                    SubjectId = schedule.SubjectId,
                    IsActive = true
                }
            });
        _mockScheduleRepository
            .Setup(repository => repository.GetSchedulesBySectionsAndSubjectsAsync(
                It.Is<IEnumerable<int>>(sectionIds => sectionIds.SequenceEqual(expectedSectionIds)),
                It.Is<IEnumerable<int>>(subjectIds => subjectIds.SequenceEqual(expectedSubjectIds))))
            .ReturnsAsync([schedule]);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionsByScheduleIdAsync(schedule.Id))
            .ReturnsAsync([session]);
        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByStudentAndSessionAsync(student.Id, session.Id))
            .ReturnsAsync((AttendanceRecord?)null);
        _mockAttendanceRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync((AttendanceRecord attendanceRecord) =>
            {
                attendanceRecord.Id = 100;
                attendanceRecord.Uuid = Guid.NewGuid();
                return attendanceRecord;
            });

        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 90
            },
            "device-secret");

        Assert.True(response.Success);
        Assert.True(response.AttendanceMarked);
        Assert.Equal(session.Uuid, response.SessionId);
        Assert.Equal("Late", response.AttendanceStatus);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_WhenDuplicateRaceHitsUniqueConstraint_ReturnsDuplicateResponse()
    {
        var service = CreateService();
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            SectionId = 1,
            IsDeleted = false
        };
        var session = new Session
        {
            Id = 11,
            Uuid = Guid.NewGuid(),
            ScheduleId = 20,
            SessionDate = DateTime.Now.Date,
            Status = SessionStatusConstants.Active,
            ActualStartTime = DateTime.Now.AddMinutes(-5),
            Schedule = new Schedules
            {
                Id = 20,
                SectionId = 1,
                SubjectId = 2,
                Section = new Section { Id = 1, Name = "BSCS 3A" },
                Subject = new Subject { Id = 2, Name = "Software Engineering" },
                Instructor = new Instructor { Id = 3, Firstname = "Ada", Lastname = "Lovelace", UserId = "inst-1" }
            }
        };

        var existingAttendance = new AttendanceRecord
        {
            Id = 99,
            Uuid = Guid.NewGuid(),
            StudentId = student.Id,
            SessionId = session.Id,
            CheckInTime = DateTime.UtcNow,
            Status = "Present",
            Student = student,
            Session = session
        };

        _context.FingerprintDevices.Add(device);
        await _context.SaveChangesAsync();

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, 5))
            .ReturnsAsync(new Fingerprint
            {
                Id = 42,
                UserId = student.UserId,
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                TemplateData = "ciphertext"
            });

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(session.Id))
            .ReturnsAsync(session);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(session.Uuid))
            .ReturnsAsync(session);
        _mockAttendanceRepository
            .SetupSequence(repository => repository.GetAttendanceByStudentAndSessionAsync(student.Id, session.Id))
            .ReturnsAsync((AttendanceRecord?)null)
            .ReturnsAsync(existingAttendance);
        _mockAttendanceRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync((AttendanceRecord record) =>
            {
                record.Id = 100;
                record.Uuid = Guid.NewGuid();
                return record;
            });
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Error", new Exception("UNIQUE constraint failed: IX_AttendanceRecords_StudentId_SessionId")));

        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 90,
                SessionId = session.Uuid
            },
            "device-secret");

        Assert.True(response.Success);
        Assert.True(response.IsDuplicateScan);
        Assert.False(response.AttendanceMarked);
        Assert.Equal(existingAttendance.Uuid, response.AttendanceRecordId);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_UsesSessionDate_WhenActualStartTimeIsNull()
    {
        var service = CreateService();
        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "user-1",
            Firstname = "John",
            Lastname = "Doe",
            SectionId = 1,
            IsDeleted = false
        };
        var sessionDate = new DateTime(2026, 1, 5);
        var session = new Session
        {
            Id = 11,
            Uuid = Guid.NewGuid(),
            ScheduleId = 20,
            SessionDate = sessionDate,
            Status = SessionStatusConstants.Active,
            ActualStartTime = null,
            Schedule = new Schedules
            {
                Id = 20,
                SectionId = 1,
                SubjectId = 2,
                TimeIn = new TimeOnly(8, 0),
                Section = new Section { Id = 1, Name = "BSCS 3A" },
                Subject = new Subject { Id = 2, Name = "Software Engineering" },
                Instructor = new Instructor { Id = 3, Firstname = "Ada", Lastname = "Lovelace", UserId = "inst-1" }
            }
        };

        DateTime capturedSessionStartTime = default;

        _context.FingerprintDevices.Add(device);
        await _context.SaveChangesAsync();

        _mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(device.DeviceIdentifier, 5))
            .ReturnsAsync(new Fingerprint
            {
                Id = 42,
                UserId = student.UserId,
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                TemplateData = "ciphertext"
            });

        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(student.Id))
            .ReturnsAsync(student);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(session.Id))
            .ReturnsAsync(session);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(session.Uuid))
            .ReturnsAsync(session);
        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByStudentAndSessionAsync(student.Id, session.Id))
            .ReturnsAsync((AttendanceRecord?)null);
        _mockAttendanceService
            .Setup(service => service.DetermineAttendanceStatus(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .Callback<DateTime, DateTime, int>((_, sessionStartTime, _) => capturedSessionStartTime = sessionStartTime)
            .Returns("Late");
        _mockAttendanceRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync((AttendanceRecord record) =>
            {
                record.Id = 100;
                record.Uuid = Guid.NewGuid();
                return record;
            });
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 90,
                SessionId = session.Uuid
            },
            "device-secret");

        Assert.True(response.Success);
        Assert.Equal(sessionDate.AddHours(8), capturedSessionStartTime);
        Assert.Equal("Late", response.AttendanceStatus);
    }

    private FingerprintService CreateService(IConfiguration? configurationOverride = null)
    {
        _mockFingerprintRepository
            .Setup(repository => repository.BeginTransactionAsync())
            .ReturnsAsync(_mockTransaction.Object);

        return new FingerprintService(
            _mockFingerprintRepository.Object,
            _mockStudentRepository.Object,
            _mockSessionRepository.Object,
            _mockScheduleRepository.Object,
            _mockStudentEnrollmentRepository.Object,
            _mockAttendanceRepository.Object,
            _mockAttendanceService.Object,
            _mockNotificationService.Object,
            _context,
            _userContextService,
            configurationOverride ?? _configuration,
            _dataProtectionProvider,
            _timeZoneProvider,
            _mockLogger.Object);
    }

    private static ClaimsPrincipal CreatePrivilegedPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, RoleConstants.Admin)
        ], "test"));
    }
}
