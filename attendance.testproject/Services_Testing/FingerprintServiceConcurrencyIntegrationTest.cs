using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Options;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

public sealed class FingerprintServiceConcurrencyIntegrationTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
    private readonly ApplicationDbContext _context;
    private readonly ConfiguredTimeZoneProvider _clock;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public FingerprintServiceConcurrencyIntegrationTest()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new FingerprintServiceSqliteTestDbContext(_dbOptions);
        _context.Database.EnsureCreated();

        _clock = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

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
    public async Task ScanFingerprintBySensorAsync_WhenDuplicateRaceOccurs_ReturnsDuplicateResponseAndPersistsScanEvent()
    {
        // Arrange
        var seed = await SeedDuplicateRaceScenarioAsync();

        var mockFingerprintRepository = new Mock<IFingerprintRepository>();
        var mockStudentRepository = new Mock<IStudentRepository>();
        var mockSessionRepository = new Mock<ISessionRepository>();
        var mockScheduleRepository = new Mock<IScheduleRepository>();
        var mockStudentEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        var mockAttendanceService = new Mock<IAttendanceService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockUserContextService = new Mock<IUserContextService>();
        var mockLogger = new Mock<ILogger<FingerprintService>>();
        var mockTransaction = new Mock<IDbContextTransaction>();

        mockTransaction
            .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransaction
            .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockFingerprintRepository
            .Setup(repository => repository.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);
        var matchedFingerprint = new Fingerprint
        {
            Id = 50,
            Uuid = Guid.NewGuid(),
            UserId = seed.Student.UserId,
            DeviceId = seed.Device.DeviceIdentifier,
            SensorFingerprintId = 5,
            TemplateData = "ciphertext"
        };

        mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(seed.Device.DeviceIdentifier, 5))
            .ReturnsAsync(matchedFingerprint);

        mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(seed.Student.UserId))
            .ReturnsAsync(seed.Student);
        mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(seed.Student.Id))
            .ReturnsAsync(seed.Student);

        mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(seed.Session.Id))
            .ReturnsAsync(seed.Session);
        mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(seed.Session.Uuid))
            .ReturnsAsync(seed.Session);

        mockAttendanceService
            .Setup(service => service.DetermineAttendanceStatus(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .Returns("Present");

        mockNotificationService
            .Setup(service => service.NotifyStudentCheckedInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var attendanceRepository = new RaceInjectingAttendanceRepository(
            _context,
            InjectCompetingAttendanceAsync);

        var service = new FingerprintService(
            mockFingerprintRepository.Object,
            mockStudentRepository.Object,
            mockSessionRepository.Object,
            mockScheduleRepository.Object,
            mockStudentEnrollmentRepository.Object,
            attendanceRepository,
            mockAttendanceService.Object,
            mockNotificationService.Object,
            _context,
            mockUserContextService.Object,
            _configuration,
            _dataProtectionProvider,
            _clock,
            mockLogger.Object);

        // Act
        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = seed.Device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 96,
                SessionId = seed.Session.Uuid
            },
            "device-secret");

        // Assert
        Assert.True(response.Success);
        Assert.True(response.IsDuplicateScan);
        Assert.False(response.AttendanceMarked);

        var persistedAttendance = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(record => record.StudentId == seed.Student.Id && record.SessionId == seed.Session.Id)
            .ToListAsync();

        var persistedScanEvents = await _context.FingerprintScanEvents
            .AsNoTracking()
            .ToListAsync();

        Assert.Single(persistedAttendance);
        Assert.Single(persistedScanEvents);

        Assert.Equal(persistedAttendance[0].Uuid, response.AttendanceRecordId);
        Assert.Equal("Duplicate", persistedScanEvents[0].Status);
        Assert.Equal(seed.Session.Id, persistedScanEvents[0].SessionId);
        Assert.Equal(persistedAttendance[0].Id, persistedScanEvents[0].AttendanceRecordId);
        Assert.NotEqual(Guid.Empty, seed.Session.Uuid);
        Assert.NotEqual(Guid.Empty, matchedFingerprint.Uuid);
        Assert.NotEqual(Guid.Empty, persistedScanEvents[0].Uuid);
        Assert.NotEqual(Guid.Empty, persistedScanEvents[0].EventId);
        Assert.NotEqual(persistedScanEvents[0].Uuid, persistedScanEvents[0].EventId);
    }

    [Fact]
    public async Task ScanFingerprintBySensorAsync_WhenDuplicateRecoveryScanEventAlreadyExists_ReturnsDuplicateResponseWithoutAddingAnother()
    {
        // Arrange
        var seed = await SeedDuplicateRaceScenarioAsync();

        var mockFingerprintRepository = new Mock<IFingerprintRepository>();
        var mockStudentRepository = new Mock<IStudentRepository>();
        var mockSessionRepository = new Mock<ISessionRepository>();
        var mockScheduleRepository = new Mock<IScheduleRepository>();
        var mockStudentEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        var mockAttendanceService = new Mock<IAttendanceService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockUserContextService = new Mock<IUserContextService>();
        var mockLogger = new Mock<ILogger<FingerprintService>>();
        var mockTransaction = new Mock<IDbContextTransaction>();

        mockTransaction
            .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransaction
            .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockFingerprintRepository
            .Setup(repository => repository.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);
        mockFingerprintRepository
            .Setup(repository => repository.FindFingerprintByDeviceAndSensorIdAsync(seed.Device.DeviceIdentifier, 5))
            .ReturnsAsync(new Fingerprint
            {
                Id = 50,
                UserId = seed.Student.UserId,
                DeviceId = seed.Device.DeviceIdentifier,
                SensorFingerprintId = 5,
                TemplateData = "ciphertext"
            });

        mockStudentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(seed.Student.UserId))
            .ReturnsAsync(seed.Student);
        mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(seed.Student.Id))
            .ReturnsAsync(seed.Student);

        mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(seed.Session.Id))
            .ReturnsAsync(seed.Session);
        mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(seed.Session.Uuid))
            .ReturnsAsync(seed.Session);

        mockAttendanceService
            .Setup(service => service.DetermineAttendanceStatus(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .Returns("Present");

        mockNotificationService
            .Setup(service => service.NotifyStudentCheckedInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var attendanceRepository = new RaceInjectingAttendanceRepository(
            _context,
            async pendingAttendance =>
            {
                var now = _clock.GetLocalNow();

                await using var competingContext = new FingerprintServiceSqliteTestDbContext(_dbOptions);

                var competingAttendance = new AttendanceRecord
                {
                    StudentId = pendingAttendance.StudentId,
                    SessionId = pendingAttendance.SessionId,
                    CheckInTime = pendingAttendance.CheckInTime,
                    Status = "Present",
                    IsManualEntry = false,
                    CreatedAt = pendingAttendance.CheckInTime,
                    UpdatedAt = pendingAttendance.CheckInTime
                };

                competingContext.AttendanceRecords.Add(competingAttendance);
                await competingContext.SaveChangesAsync();

                competingContext.FingerprintScanEvents.Add(new FingerprintScanEvent
                {
                    DeviceId = seed.Device.Id,
                    SessionId = seed.Session.Id,
                    MatchedStudentId = seed.Student.Id,
                    AttendanceRecordId = competingAttendance.Id,
                    Status = "Duplicate",
                    MatchScore = 0.96m,
                    CapturedAt = now,
                    ReceivedAt = now,
                    CreatedAt = now,
                });
                await competingContext.SaveChangesAsync();
            });

        var service = new FingerprintService(
            mockFingerprintRepository.Object,
            mockStudentRepository.Object,
            mockSessionRepository.Object,
            mockScheduleRepository.Object,
            mockStudentEnrollmentRepository.Object,
            attendanceRepository,
            mockAttendanceService.Object,
            mockNotificationService.Object,
            _context,
            mockUserContextService.Object,
            _configuration,
            _dataProtectionProvider,
            _clock,
            mockLogger.Object);

        // Act
        var response = await service.ScanFingerprintBySensorAsync(
            new ScanFingerprintBySensorRequest
            {
                DeviceId = seed.Device.DeviceIdentifier,
                SensorFingerprintId = 5,
                Confidence = 96,
                SessionId = seed.Session.Uuid
            },
            "device-secret");

        // Assert
        Assert.True(response.Success);
        Assert.True(response.IsDuplicateScan);

        var persistedAttendance = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(record => record.StudentId == seed.Student.Id && record.SessionId == seed.Session.Id)
            .ToListAsync();

        var persistedScanEvents = await _context.FingerprintScanEvents
            .AsNoTracking()
            .ToListAsync();

        Assert.Single(persistedAttendance);
        Assert.Single(persistedScanEvents);
        Assert.Equal(seed.Session.Id, persistedScanEvents[0].SessionId);
        Assert.Equal(persistedAttendance[0].Id, persistedScanEvents[0].AttendanceRecordId);
        Assert.Equal(persistedAttendance[0].Uuid, response.AttendanceRecordId);
        Assert.NotEqual(Guid.Empty, persistedScanEvents[0].Uuid);
        Assert.NotEqual(Guid.Empty, persistedScanEvents[0].EventId);
        Assert.NotEqual(persistedScanEvents[0].Uuid, persistedScanEvents[0].EventId);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(Student Student, Session Session, FingerprintDevice Device)> SeedDuplicateRaceScenarioAsync()
    {
        var now = _clock.GetLocalNow();

        var studentUser = new IdentityUser
        {
            Id = "student-user-1",
            UserName = "student-user-1",
            NormalizedUserName = "STUDENT-USER-1",
            Email = "student1@example.com",
            NormalizedEmail = "STUDENT1@EXAMPLE.COM"
        };

        var instructorUser = new IdentityUser
        {
            Id = "instructor-user-1",
            UserName = "instructor-user-1",
            NormalizedUserName = "INSTRUCTOR-USER-1",
            Email = "instructor1@example.com",
            NormalizedEmail = "INSTRUCTOR1@EXAMPLE.COM"
        };

        var course = new Course
        {
            Id = 1,
            Name = "BSCS",
            CreatedAt = now,
            UpdatedAt = now
        };

        var section = new Section
        {
            Id = 1,
            Name = "BSCS 3A",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var subject = new Subject
        {
            Id = 2,
            Name = "Software Engineering",
            Code = "CSSE1",
            CreatedAt = now,
            UpdatedAt = now
        };

        var classroom = new Classroom
        {
            Id = 1,
            Name = "Room 301",
            CreatedAt = now,
            UpdatedAt = now
        };

        var instructor = new Instructor
        {
            Id = 3,
            UserId = instructorUser.Id,
            Firstname = "Ada",
            Lastname = "Lovelace",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        var schedule = new Schedules
        {
            Id = 20,
            TimeIn = TimeOnly.FromDateTime(now.AddMinutes(-30)),
            TimeOut = TimeOnly.FromDateTime(now.AddMinutes(60)),
            DayOfWeek = now.DayOfWeek.ToString(),
            SubjectId = subject.Id,
            ClassroomId = classroom.Id,
            SectionId = section.Id,
            InstructorId = instructor.Id,
            Subject = subject,
            Classroom = classroom,
            Section = section,
            Instructor = instructor,
            CreatedAt = now,
            UpdatedAt = now
        };

        var student = new Student
        {
            Id = 1,
            UserId = studentUser.Id,
            Firstname = "John",
            Lastname = "Doe",
            SectionId = section.Id,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        var session = new Session
        {
            Id = 11,
            ScheduleId = schedule.Id,
            SessionDate = now.Date,
            Status = SessionStatusConstants.Active,
            ActualStartTime = now.AddMinutes(-5),
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = [1],
            Schedule = schedule
        };

        var device = new FingerprintDevice
        {
            Id = 7,
            DeviceIdentifier = "esp32-attendance-01",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.AddRange(studentUser, instructorUser);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Subjects.Add(subject);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Schedules.Add(schedule);
        _context.Students.Add(student);
        _context.Sessions.Add(session);
        _context.FingerprintDevices.Add(device);

        await _context.SaveChangesAsync();

        return (student, session, device);
    }

    private async Task InjectCompetingAttendanceAsync(AttendanceRecord pendingAttendance)
    {
        await using var competingContext = new FingerprintServiceSqliteTestDbContext(_dbOptions);

        competingContext.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentId = pendingAttendance.StudentId,
            SessionId = pendingAttendance.SessionId,
            CheckInTime = pendingAttendance.CheckInTime,
            Status = "Present",
            IsManualEntry = false,
            CreatedAt = pendingAttendance.CheckInTime,
            UpdatedAt = pendingAttendance.CheckInTime
        });

        await competingContext.SaveChangesAsync();
    }

    private sealed class RaceInjectingAttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendanceRepository _inner;
        private readonly Func<AttendanceRecord, Task> _injectConflictAsync;
        private bool _raceInjected;

        public RaceInjectingAttendanceRepository(
            ApplicationDbContext context,
            Func<AttendanceRecord, Task> injectConflictAsync)
        {
            _context = context;
            _inner = new AttendanceRepository(context);
            _injectConflictAsync = injectConflictAsync;
        }

        public Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance)
            => _inner.CreateAsync(attendance);

        public Task<List<AttendanceRecord>> CreateBulkAsync(List<AttendanceRecord> attendanceRecords)
            => _inner.CreateBulkAsync(attendanceRecords);

        public Task<AttendanceRecord?> GetByIdAsync(int id)
            => _inner.GetByIdAsync(id);

        public Task<AttendanceRecord?> GetByIdTrackedAsync(int id)
            => _inner.GetByIdTrackedAsync(id);

        public Task<AttendanceRecord?> GetAttendanceByUuidAsync(Guid uuid)
            => _inner.GetAttendanceByUuidAsync(uuid);

        public Task<AttendanceRecord?> GetAttendanceByUuidTrackedAsync(Guid uuid)
            => _inner.GetAttendanceByUuidTrackedAsync(uuid);

        public Task<List<AttendanceRecord>> GetAllAsync(int pageNumber = 1, int pageSize = 50)
            => _inner.GetAllAsync(pageNumber, pageSize);

        public Task<List<AttendanceRecord>> GetByStudentIdAsync(int studentId)
            => _inner.GetByStudentIdAsync(studentId);

        public Task<List<AttendanceRecord>> GetBySessionIdAsync(int sessionId)
            => _inner.GetBySessionIdAsync(sessionId);

        public Task<List<SessionAttendanceRosterDto>> GetBySessionIdForRosterAsync(int sessionId)
            => _inner.GetBySessionIdForRosterAsync(sessionId);

        public Task<AttendanceRecord?> GetBySessionAndStudentAsync(int sessionId, int studentId)
            => _inner.GetBySessionAndStudentAsync(sessionId, studentId);

        public Task<AttendanceMinimalDto?> GetBySessionAndStudentMinimalAsync(int sessionId, int studentId)
            => _inner.GetBySessionAndStudentMinimalAsync(sessionId, studentId);

        public Task<List<AttendanceRecord>> GetByStudentAndDateRangeAsync(int studentId, DateTime startDate, DateTime endDate)
            => _inner.GetByStudentAndDateRangeAsync(studentId, startDate, endDate);

        public Task<List<AttendanceRecord>> GetBySessionIdsAsync(List<int> sessionIds)
            => _inner.GetBySessionIdsAsync(sessionIds);

        public Task<AttendanceRecord> UpdateAsync(AttendanceRecord attendance)
            => _inner.UpdateAsync(attendance);

        public Task<bool> DeleteAsync(int id)
            => _inner.DeleteAsync(id);

        public Task<bool> HasAttendanceRecordAsync(int studentId, int sessionId)
            => _inner.HasAttendanceRecordAsync(studentId, sessionId);

        public Task<bool> HasAnyAttendanceAsync(int studentId)
            => _inner.HasAnyAttendanceAsync(studentId);

        public Task<bool> SessionHasAttendanceAsync(int sessionId)
            => _inner.SessionHasAttendanceAsync(sessionId);

        public Task<AttendanceRecord?> GetAttendanceByStudentAndSessionAsync(int studentId, int sessionId)
            => _inner.GetAttendanceByStudentAndSessionAsync(studentId, sessionId);

        public Task<int> GetAttendanceCountAsync(int studentId, string? status = null)
            => _inner.GetAttendanceCountAsync(studentId, status);

        #pragma warning disable CS0618
        public Task<List<AttendanceRecordResponseDto>> GetAllForListingAsync(int pageNumber = 1, int pageSize = 50)
            => _inner.GetAllForListingAsync(pageNumber, pageSize);
        #pragma warning restore CS0618

        public Task<List<AttendanceListDto>> GetAllForListingOptimizedAsync(int pageNumber = 1, int pageSize = 50)
            => _inner.GetAllForListingOptimizedAsync(pageNumber, pageSize);

        public Task<(List<AttendanceRecord> Records, int TotalCount)> GetFilteredAsync(
            int? studentId = null,
            int? sessionId = null,
            int? scheduleId = null,
            int? sectionId = null,
            int? subjectId = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isManualEntry = null,
            int pageNumber = 1,
            int pageSize = 50)
            => _inner.GetFilteredAsync(
                studentId,
                sessionId,
                scheduleId,
                sectionId,
                subjectId,
                status,
                startDate,
                endDate,
                isManualEntry,
                pageNumber,
                pageSize);

        public Task<(int Total, int Present, int Late, int Absent, int Excused, long AvgCheckInTicks)> GetStatisticsAsync(
            int? studentId = null,
            int? sessionId = null,
            int? scheduleId = null,
            int? sectionId = null,
            int? subjectId = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isManualEntry = null)
            => _inner.GetStatisticsAsync(
                studentId,
                sessionId,
                scheduleId,
                sectionId,
                subjectId,
                status,
                startDate,
                endDate,
                isManualEntry);

        public async Task<int> SaveChangesAsync()
        {
            if (!_raceInjected)
            {
                var pendingAttendance = _context.ChangeTracker
                    .Entries<AttendanceRecord>()
                    .Where(entry => entry.State == EntityState.Added)
                    .Select(entry => entry.Entity)
                    .FirstOrDefault();

                if (pendingAttendance != null)
                {
                    _raceInjected = true;
                    await _injectConflictAsync(pendingAttendance);
                }
            }

            return await _inner.SaveChangesAsync();
        }
    }

    private sealed class FingerprintServiceSqliteTestDbContext : ApplicationDbContext
    {
        public FingerprintServiceSqliteTestDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SQLite does not support SQL Server rowversion generation semantics.
            builder.Entity<Session>()
                .Property(e => e.RowVersion)
                .IsRequired(false)
                .ValueGeneratedNever()
                .IsConcurrencyToken(false);

            builder.Entity<FingerprintScanEvent>()
                .Property(e => e.RowVersion)
                .IsRequired(false)
                .ValueGeneratedNever()
                .IsConcurrencyToken(false);
        }
    }
}
