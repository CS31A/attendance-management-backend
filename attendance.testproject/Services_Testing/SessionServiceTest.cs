using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using attendance_monitoring.Options;
using System.Security.Claims;
using System.Text.Json;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for SessionService
/// </summary>
public class SessionServiceTest
{
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IScheduleRepository> _mockScheduleRepository;
    private readonly Mock<IInstructorRepository> _mockInstructorRepository;
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;
    private readonly Mock<IStudentEnrollmentRepository> _mockEnrollmentRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<SessionService>> _mockLogger;
    private readonly ConfiguredTimeZoneProvider _timeZoneProvider;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly UserContextService _userContextService;
    private readonly SessionService _sessionService;
    private readonly ClaimsPrincipal _testUser;
    private readonly DefaultHttpContext _httpContext;

    public SessionServiceTest()
    {
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockScheduleRepository = new Mock<IScheduleRepository>();
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockClassroomRepository = new Mock<IClassroomRepository>();
        _mockEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<SessionService>>();

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

        // Setup test user with claims
        _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Role, RoleConstants.Instructor)
        }, "TestAuthentication"));

        // Setup HTTP context
        _httpContext = new DefaultHttpContext
        {
            User = _testUser
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        _timeZoneProvider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        _sessionService = new SessionService(
            _mockSessionRepository.Object,
            _mockScheduleRepository.Object,
            _mockInstructorRepository.Object,
            _mockClassroomRepository.Object,
            _mockEnrollmentRepository.Object,
            _mockNotificationService.Object,
            _userContextService,
            _mockHttpContextAccessor.Object,
            _timeZoneProvider,
            _mockLogger.Object
        );
    }

    #region GetSessionByIdAsync Tests

    [Fact]
    public async Task GetSessionByIdAsync_ReturnsSessionDto_WhenSessionExists()
    {
        // Arrange
        int sessionId = 1;
        var session = CreateTestSession(sessionId);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByIdAsync(1))
            .ReturnsAsync(CreateTestClassroom(1));

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByIdAsync(1))
            .ReturnsAsync(CreateTestClassroom(1));

        // Act
        var result = await _sessionService.GetSessionByIdAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Uuid, result.Id);
        Assert.Equal(SessionStatusConstants.NotStarted, result.Status);
        _mockSessionRepository.Verify(r => r.GetSessionByIdAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task GetSessionByIdAsync_ThrowsEntityNotFoundException_WhenSessionDoesNotExist()
    {
        // Arrange
        int sessionId = 999;
        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _sessionService.GetSessionByIdAsync(sessionId));
    }

    [Fact]
    public async Task GetSessionByIdAsync_ThrowsEntityServiceException_WhenRepositoryThrowsException()
    {
        // Arrange
        int sessionId = 1;
        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<EntityServiceException>(
            () => _sessionService.GetSessionByIdAsync(sessionId));
    }

    #endregion

    #region GetSessionByUuidAsync Tests

    [Fact]
    public async Task GetSessionByUuidAsync_ReturnsSessionDto_WhenFound()
    {
        // Arrange
        var session = CreateTestSession(1);
        session.Uuid = Guid.NewGuid();

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(session.Uuid))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.GetSessionByUuidAsync(session.Uuid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Uuid, result.Id);
        _mockSessionRepository.Verify(r => r.GetSessionByUuidAsync(session.Uuid), Times.Once);
    }

    [Fact]
    public async Task GetSessionByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        // Arrange
        var sessionUuid = Guid.NewGuid();

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.GetSessionByUuidAsync(sessionUuid));
    }

    #endregion

    #region GetAllSessionsAsync Tests

    [Fact]
    public async Task GetAllSessionsAsync_ReturnsAllSessions()
    {
        // Arrange
        var sessions = new List<Session>
        {
            CreateTestSession(1),
            CreateTestSession(2),
            CreateTestSession(3)
        };

        _mockSessionRepository
            .Setup(r => r.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetAllSessionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockSessionRepository.Verify(r => r.GetAllSessionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSessionsAsync_ReturnsEmptyList_WhenNoSessions()
    {
        // Arrange
        _mockSessionRepository
            .Setup(r => r.GetAllSessionsAsync())
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _sessionService.GetAllSessionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetSessionsByScheduleIdAsync Tests

    [Fact]
    public async Task GetSessionsByScheduleIdAsync_ReturnsSessionsForSchedule()
    {
        // Arrange
        int scheduleId = 1;
        var sessions = new List<Session>
        {
            CreateTestSession(1, scheduleId),
            CreateTestSession(2, scheduleId)
        };

        _mockSessionRepository
            .Setup(r => r.GetSessionsByScheduleIdAsync(scheduleId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetSessionsByScheduleIdAsync(scheduleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region GetSessionsByStatusAsync Tests

    [Fact]
    public async Task GetSessionsByStatusAsync_ReturnsSessionsWithStatus()
    {
        // Arrange
        string status = SessionStatusConstants.Active;
        var sessions = new List<Session>
        {
            CreateTestSession(1, status: SessionStatusConstants.Active),
            CreateTestSession(2, status: SessionStatusConstants.Active)
        };

        _mockSessionRepository
            .Setup(r => r.GetSessionsByStatusAsync(status))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetSessionsByStatusAsync(status);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Equal(SessionStatusConstants.Active, s.Status));
    }

    #endregion

    #region GetSessionsByDateAsync Tests

    [Fact]
    public async Task GetSessionsByDateAsync_ReturnsSessionsForDate()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var sessions = new List<Session>
        {
            CreateTestSession(1, sessionDate: date),
            CreateTestSession(2, sessionDate: date)
        };

        _mockSessionRepository
            .Setup(r => r.GetSessionsByDateAsync(date))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionService.GetSessionsByDateAsync(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_CreatesSession_WhenValidRequest()
    {
        // Arrange
        // Calculate next Monday
        var today = _timeZoneProvider.GetLocalNow().Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, get next Monday
        var nextMonday = today.AddDays(daysUntilMonday);

        var schedule = CreateTestSchedule(1, "Monday");
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = nextMonday,
            Description = "Test session"
        };
        var createdSession = CreateTestSession(1, schedule.Id, sessionDate: request.SessionDate);
        createdSession.Schedule = schedule;

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(false);

        _mockSessionRepository
            .Setup(r => r.CreateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                // Simulate database assigning an ID and setting timestamps
                createdSession.Status = s.Status;
                createdSession.ScheduleId = s.ScheduleId;
                createdSession.SessionDate = s.SessionDate;
                createdSession.Description = s.Description;
            })
            .ReturnsAsync(createdSession);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(() => createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);

        // Verify schedule ID
        Assert.Equal(request.ScheduleId, result.ScheduleId);

        // Verify initial status
        Assert.Equal(SessionStatusConstants.NotStarted, result.Status);

        // Verify session date
        Assert.Equal(request.SessionDate, result.SessionDate);

        // Verify description
        Assert.Equal(request.Description, result.Description);

        // Verify repository method was called with correct session data
        _mockSessionRepository.Verify(r => r.CreateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.NotStarted &&
                s.ScheduleId == schedule.Id &&
                s.SessionDate == request.SessionDate
            )), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsEntityNotFoundException_WhenScheduleDoesNotExist()
    {
        // Arrange
        var request = new CreateSession
        {
            ScheduleId = Guid.NewGuid(),
            SessionDate = _timeZoneProvider.GetLocalNow().Date.AddDays(1)
        };

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync((Schedules?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsEntityAlreadyExistsException_WhenSessionAlreadyExists()
    {
        // Arrange
        var schedule = CreateTestSchedule(1, "Monday");
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = _timeZoneProvider.GetLocalNow().Date.AddDays(1)
        };

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<Guid>>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsValidationException_WhenDateDoesNotMatchScheduleDayOfWeek()
    {
        // Arrange
        var mondayDate = new DateTime(2024, 1, 15); // Monday
        var schedule = CreateTestSchedule(1, "Tuesday"); // Schedule is for Tuesday
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = mondayDate
        };

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesSession_WhenDateDoesNotMatchButOffScheduleOverrideProvided()
    {
        // Arrange
        var today = _timeZoneProvider.GetLocalNow().Date;
        var daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilTuesday == 0) daysUntilTuesday = 7;
        var nextTuesday = today.AddDays(daysUntilTuesday);

        var schedule = CreateTestSchedule(1, "Monday");
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = nextTuesday,
            AllowOffScheduleDate = true,
            OffScheduleReason = "Campus activity moved this class"
        };
        var createdSession = CreateTestSession(1, schedule.Id, sessionDate: request.SessionDate);
        createdSession.Schedule = schedule;

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(false);

        _mockSessionRepository
            .Setup(r => r.CreateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                createdSession.Status = s.Status;
                createdSession.ScheduleId = s.ScheduleId;
                createdSession.SessionDate = s.SessionDate;
                createdSession.Description = s.Description;
            })
            .ReturnsAsync(createdSession);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.ScheduleId, result.ScheduleId);
        Assert.Equal(request.SessionDate, result.SessionDate);
        Assert.Equal(SessionStatusConstants.NotStarted, result.Status);

        _mockSessionRepository.Verify(r => r.CreateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.NotStarted &&
                s.ScheduleId == schedule.Id &&
                s.SessionDate == request.SessionDate
            )), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateSessionAsync_AllowsEmptyOffScheduleReason_WhenDateMatchesSchedule(string? offScheduleReason)
    {
        // Arrange
        var today = _timeZoneProvider.GetLocalNow().Date;
        var matchingDate = today.AddDays(((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7);
        var schedule = CreateTestSchedule(1, matchingDate.DayOfWeek.ToString());
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = matchingDate,
            OffScheduleReason = offScheduleReason
        };
        var createdSession = CreateTestSession(1, schedule.Id, sessionDate: request.SessionDate);
        createdSession.Schedule = schedule;

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(false);

        _mockSessionRepository
            .Setup(r => r.CreateSessionAsync(It.IsAny<Session>()))
            .ReturnsAsync(createdSession);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.Equal(request.ScheduleId, result.ScheduleId);
        Assert.Equal(matchingDate, result.SessionDate);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsValidationException_WhenDateIsInPast()
    {
        // Arrange
        var pastDate = _timeZoneProvider.GetLocalNow().Date.AddDays(-1);
        var schedule = CreateTestSchedule(1, pastDate.DayOfWeek.ToString());
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = pastDate
        };

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, request.SessionDate!.Value))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("past date", exception.Message);
    }

    #endregion

    #region StartSessionAsync Tests

    [Fact]
    public async Task StartSessionAsync_StartsSession_WhenValidRequest()
    {
        // Arrange
        int sessionId = 1;
        var classroom = CreateTestClassroom(1);
        var request = new StartSession
        {
            ActualRoomId = classroom.Uuid,
            AttendanceCutoffMinutes = 15,
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted, sessionDate: DateTime.Now.Date);
        session.Schedule = CreateTestSchedule(1, DateTime.Now.DayOfWeek.ToString());
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId.Value))
            .ReturnsAsync(classroom);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                // Simulate the session being updated in the database
                session.Status = s.Status;
                session.ActualStartTime = s.ActualStartTime;
                session.ActualRoomId = s.ActualRoomId;
                session.AttendanceCutOff = s.AttendanceCutOff;
                session.StartedBy = s.StartedBy;
                session.ActualRoom = classroom;
                session.InstructorWhoStarted = instructor;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(() => session); // Return the updated session

        // Act
        var beforeStart = DateTime.Now;
        var result = await _sessionService.StartSessionAsync(sessionId, request);
        var afterStart = DateTime.Now;

        // Assert
        Assert.NotNull(result);

        // Verify status change
        Assert.Equal(SessionStatusConstants.Active, result.Status);

        // Verify timestamps
        Assert.NotNull(result.ActualStartTime);
        Assert.InRange(result.ActualStartTime.Value, beforeStart.AddSeconds(-2), afterStart.AddSeconds(2));

        // Verify room assignment
        Assert.Equal(request.ActualRoomId, result.ActualRoomId);

        // Verify instructor tracking
        Assert.Equal(instructor.Uuid, result.StartedById);

        // Verify attendance cutoff calculation
        Assert.NotNull(result.AttendanceCutOff);
        var expectedCutoff = result.ActualStartTime.Value.AddMinutes(request.AttendanceCutoffMinutes ?? 15);
        Assert.Equal(expectedCutoff, result.AttendanceCutOff.Value);

        // Verify repository method was called with correct session state
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.Active &&
                s.ActualStartTime != null &&
                s.StartedBy == instructor.Id &&
                s.ActualRoomId == classroom.Id &&
                s.RowVersion != null &&
                s.RowVersion.SequenceEqual(request.RowVersion)
            )), Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsValidationException_WhenRowVersionMissing()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted, sessionDate: DateTime.Now.Date);
        session.Schedule = CreateTestSchedule(1, DateTime.Now.DayOfWeek.ToString());
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByIdAsync(1))
            .ReturnsAsync(CreateTestClassroom(1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("rowVersion", exception.Message, StringComparison.OrdinalIgnoreCase);
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsEntityUnauthorizedException_WhenUserContextNotFound()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("User context not found", exception.Message);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsEntityUnauthorizedException_WhenInstructorNotFound()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("Instructor profile not found", exception.Message);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsEntityUnauthorizedException_WhenInstructorNotAuthorized()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = 999; // Different instructor

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("not authorized", exception.Message);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsValidationException_WhenSessionAlreadyActive()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("already been started", exception.Message);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsValidationException_WhenSessionDateNotToday()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession();

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted, sessionDate: DateTime.Now.Date.AddDays(1));
        session.Schedule = CreateTestSchedule(1, DateTime.Now.AddDays(1).DayOfWeek.ToString());
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
        Assert.Contains("scheduled for", exception.Message);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsEntityConflictException_WhenConcurrentUpdateDetected()
    {
        // Arrange
        int sessionId = 1;
        var request = new StartSession
        {
            AttendanceCutoffMinutes = 15,
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var classroom = CreateTestClassroom(1);
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted, sessionDate: DateTime.Now.Date);
        session.Schedule = CreateTestSchedule(1, DateTime.Now.DayOfWeek.ToString());
        session.Schedule.InstructorId = instructor.Id;

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByIdAsync(1))
            .ReturnsAsync(classroom);

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Session row was modified by another request."));

        // Act & Assert
        await Assert.ThrowsAsync<EntityConflictException>(
            () => _sessionService.StartSessionAsync(sessionId, request));
    }

    [Fact]
    public async Task StartSessionByUuidAsync_StartsSession_WhenValid()
    {
        // Arrange
        int sessionId = 1;
        var sessionUuid = Guid.NewGuid();
        var classroom = CreateTestClassroom(1);
        var request = new StartSession
        {
            ActualRoomId = classroom.Uuid,
            AttendanceCutoffMinutes = 15,
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted, sessionDate: DateTime.Now.Date);
        session.Uuid = sessionUuid;
        session.Schedule = CreateTestSchedule(1, DateTime.Now.DayOfWeek.ToString());
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId.Value))
            .ReturnsAsync(classroom);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(updated =>
            {
                session.Status = updated.Status;
                session.ActualStartTime = updated.ActualStartTime;
                session.ActualRoomId = updated.ActualRoomId;
                session.AttendanceCutOff = updated.AttendanceCutOff;
                session.StartedBy = updated.StartedBy;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.StartSessionByUuidAsync(sessionUuid, request);

        // Assert
        Assert.Equal(SessionStatusConstants.Active, result.Status);
        Assert.Equal(session.Uuid, result.Id);
        _mockSessionRepository.Verify(r => r.GetSessionByUuidAsync(sessionUuid), Times.Once);
    }

    [Fact]
    public async Task StartSessionByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        // Arrange
        var sessionUuid = Guid.NewGuid();

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.StartSessionByUuidAsync(sessionUuid, new StartSession()));
    }

    #endregion

    #region EndSessionAsync Tests

    [Fact]
    public async Task EndSessionAsync_EndsSession_WhenValidRequest()
    {
        // Arrange
        int sessionId = 1;
        var request = new EndSession
        {
            Description = "Session ended successfully",
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                // Simulate the session being updated in the database
                session.Status = s.Status;
                session.ActualEndTime = s.ActualEndTime;
                session.EndedBy = s.EndedBy;
                session.Description = s.Description;
                session.InstructorWhoEnded = instructor;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(() => session); // Return the updated session

        // Act
        var beforeEnd = DateTime.Now;
        var result = await _sessionService.EndSessionAsync(sessionId, request);
        var afterEnd = DateTime.Now;

        // Assert
        Assert.NotNull(result);

        // Verify status change
        Assert.Equal(SessionStatusConstants.Ended, result.Status);

        // Verify end timestamp
        Assert.NotNull(result.ActualEndTime);
        Assert.InRange(result.ActualEndTime.Value, beforeEnd.AddSeconds(-2), afterEnd.AddSeconds(2));

        // Verify instructor tracking
        Assert.Equal(instructor.Uuid, result.EndedById);

        // Verify description was updated
        Assert.Contains(request.Description, result.Description);

        // Verify repository method was called with correct session state
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.Ended &&
                s.ActualEndTime != null &&
                s.EndedBy == instructor.Id &&
                s.RowVersion != null &&
                s.RowVersion.SequenceEqual(request.RowVersion)
            )), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_ThrowsValidationException_WhenRowVersionMissing()
    {
        // Arrange
        int sessionId = 1;
        var request = new EndSession();

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.EndSessionAsync(sessionId, request));
        Assert.Contains("rowVersion", exception.Message, StringComparison.OrdinalIgnoreCase);
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task EndSessionAsync_ThrowsValidationException_WhenSessionNotActive()
    {
        // Arrange
        int sessionId = 1;
        var request = new EndSession
        {
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.EndSessionAsync(sessionId, request));
        Assert.Contains("not been started", exception.Message);
    }

    [Fact]
    public async Task EndSessionAsync_ThrowsEntityConflictException_WhenConcurrentUpdateDetected()
    {
        // Arrange
        int sessionId = 1;
        var request = new EndSession
        {
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Session row was modified by another request."));

        // Act & Assert
        await Assert.ThrowsAsync<EntityConflictException>(
            () => _sessionService.EndSessionAsync(sessionId, request));

        _mockSessionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EndSessionByUuidAsync_EndsSession_WhenValid()
    {
        // Arrange
        int sessionId = 1;
        var sessionUuid = Guid.NewGuid();
        var request = new EndSession
        {
            Description = "Session ended successfully",
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Uuid = sessionUuid;
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(updated =>
            {
                session.Status = updated.Status;
                session.ActualEndTime = updated.ActualEndTime;
                session.EndedBy = updated.EndedBy;
                session.Description = updated.Description;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.EndSessionByUuidAsync(sessionUuid, request);

        // Assert
        Assert.Equal(SessionStatusConstants.Ended, result.Status);
        Assert.Equal(session.Uuid, result.Id);
        _mockSessionRepository.Verify(r => r.GetSessionByUuidAsync(sessionUuid), Times.Once);
    }

    [Fact]
    public async Task EndSessionByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        // Arrange
        var sessionUuid = Guid.NewGuid();

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.EndSessionByUuidAsync(sessionUuid, new EndSession()));
    }

    #endregion

    #region CancelSessionAsync Tests

    [Fact]
    public async Task CancelSessionAsync_CancelsSession_WhenValidRequest()
    {
        // Arrange
        int sessionId = 1;
        var request = new CancelSession
        {
            Reason = "Instructor unavailable",
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                // Simulate the session being updated in the database
                session.Status = s.Status;
                session.Description = s.Description;
                session.RowVersion = s.RowVersion;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(() => session); // Return the updated session

        // Act
        var result = await _sessionService.CancelSessionAsync(sessionId, request);

        // Assert
        Assert.NotNull(result);

        // Verify status change
        Assert.Equal(SessionStatusConstants.Cancelled, result.Status);

        // Verify cancellation reason was recorded
        Assert.Contains(request.Reason, result.Description!);

        // Verify repository method was called with correct session state
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.Cancelled &&
                s.Description != null && s.Description.Contains(request.Reason) &&
                s.RowVersion != null &&
                s.RowVersion.SequenceEqual(request.RowVersion)
            )), Times.Once);
    }

    [Fact]
    public async Task CancelSessionAsync_ThrowsValidationException_WhenRowVersionMissing()
    {
        // Arrange
        int sessionId = 1;
        var request = new CancelSession { Reason = "Scheduling conflict" };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.CancelSessionAsync(sessionId, request));
        Assert.Contains("rowVersion", exception.Message, StringComparison.OrdinalIgnoreCase);
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task CancelSessionAsync_ThrowsValidationException_WhenSessionActive()
    {
        // Arrange
        int sessionId = 1;
        var request = new CancelSession { Reason = "Test" };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.CancelSessionAsync(sessionId, request));
        Assert.Contains("active session", exception.Message);
    }

    [Fact]
    public async Task CancelSessionAsync_ThrowsEntityConflictException_WhenConcurrentUpdateDetected()
    {
        // Arrange
        int sessionId = 1;
        var request = new CancelSession
        {
            Reason = "Scheduling conflict",
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Session row was modified by another request."));

        // Act & Assert
        await Assert.ThrowsAsync<EntityConflictException>(
            () => _sessionService.CancelSessionAsync(sessionId, request));

        _mockSessionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelSessionByUuidAsync_CancelsSession_WhenValid()
    {
        // Arrange
        int sessionId = 1;
        var sessionUuid = Guid.NewGuid();
        var request = new CancelSession
        {
            Reason = "Instructor unavailable",
            RowVersion = CreateRowVersion()
        };

        var instructor = CreateTestInstructor(1, "user-123");
        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);
        session.Uuid = sessionUuid;
        session.Schedule = CreateTestSchedule(1, "Monday");
        session.Schedule.InstructorId = instructor.Id;

        _mockInstructorRepository
            .Setup(r => r.GetInstructorByUserIdAsync("user-123"))
            .ReturnsAsync(instructor);

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                session.Status = s.Status;
                session.Description = s.Description;
                session.RowVersion = s.RowVersion;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(() => session); // Return the updated session

        // Act
        var result = await _sessionService.CancelSessionByUuidAsync(sessionUuid, request);

        // Assert
        Assert.NotNull(result);

        // Verify status change
        Assert.Equal(SessionStatusConstants.Cancelled, result.Status);

        // Verify cancellation reason was recorded
        Assert.Contains(request.Reason, result.Description!);

        // Verify repository method was called with correct session state
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(
            It.Is<Session>(s =>
                s.Status == SessionStatusConstants.Cancelled &&
                s.Description != null && s.Description.Contains(request.Reason) &&
                s.RowVersion != null &&
                s.RowVersion.SequenceEqual(request.RowVersion)
            )), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionRoomAsync_ThrowsValidationException_WhenRowVersionMissing()
    {
        // Arrange
        int sessionId = 1;
        var classroom = CreateTestClassroom(2);
        var request = new UpdateSessionRoom { ActualRoomId = classroom.Uuid };

        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId!.Value))
            .ReturnsAsync(classroom);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.UpdateSessionRoomAsync(sessionId, request));
        Assert.Contains("rowVersion", exception.Message, StringComparison.OrdinalIgnoreCase);
        _mockSessionRepository.Verify(r => r.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSessionRoomAsync_ThrowsValidationException_WhenSessionNotActive()
    {
        // Arrange
        int sessionId = 1;
        var classroom = CreateTestClassroom(2);
        var request = new UpdateSessionRoom
        {
            ActualRoomId = classroom.Uuid,
            RowVersion = CreateRowVersion()
        };

        var session = CreateTestSession(sessionId, status: SessionStatusConstants.NotStarted);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId!.Value))
            .ReturnsAsync(classroom);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionService.UpdateSessionRoomAsync(sessionId, request));
        Assert.Contains("not started", exception.Message);
    }

    [Fact]
    public async Task UpdateSessionRoomAsync_ThrowsEntityNotFoundException_WhenClassroomNotFound()
    {
        // Arrange
        int sessionId = 1;
        var request = new UpdateSessionRoom { ActualRoomId = Guid.NewGuid() };

        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId!.Value))
            .ReturnsAsync((Classroom?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.UpdateSessionRoomAsync(sessionId, request));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateSessionRoomAsync_ThrowsEntityConflictException_WhenConcurrentUpdateDetected()
    {
        // Arrange
        int sessionId = 1;
        var classroom = CreateTestClassroom(2);
        var request = new UpdateSessionRoom
        {
            ActualRoomId = classroom.Uuid,
            RowVersion = CreateRowVersion()
        };

        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId!.Value))
            .ReturnsAsync(classroom);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Session row was modified by another request."));

        // Act & Assert
        await Assert.ThrowsAsync<EntityConflictException>(
            () => _sessionService.UpdateSessionRoomAsync(sessionId, request));

        _mockSessionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateSessionRoomByUuidAsync_UpdatesRoom_WhenSessionActive()
    {
        // Arrange
        int sessionId = 1;
        var sessionUuid = Guid.NewGuid();
        var classroom = CreateTestClassroom(2);
        var request = new UpdateSessionRoom
        {
            ActualRoomId = classroom.Uuid,
            RowVersion = CreateRowVersion()
        };

        var session = CreateTestSession(sessionId, status: SessionStatusConstants.Active);
        session.Uuid = sessionUuid;

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockClassroomRepository
            .Setup(r => r.GetClassroomByUuidAsync(request.ActualRoomId!.Value))
            .ReturnsAsync(classroom);

        _mockSessionRepository
            .Setup(r => r.UpdateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(updated =>
            {
                session.ActualRoomId = updated.ActualRoomId;
                session.ActualRoom = classroom;
            })
            .ReturnsAsync(session);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.UpdateSessionRoomByUuidAsync(sessionUuid, request);

        // Assert
        Assert.Equal(request.ActualRoomId, result.ActualRoomId);
        Assert.Equal(session.Uuid, result.Id);
        _mockSessionRepository.Verify(r => r.GetSessionByUuidAsync(sessionUuid), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionRoomByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        // Arrange
        var sessionUuid = Guid.NewGuid();

        _mockSessionRepository
            .Setup(r => r.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionService.UpdateSessionRoomByUuidAsync(sessionUuid, new UpdateSessionRoom()));
    }



    #endregion

    #region Helper Methods

    private Session CreateTestSession(
        int id,
        int scheduleId = 1,
        string status = SessionStatusConstants.NotStarted,
        DateTime? sessionDate = null)
    {
        return new Session
        {
            Id = id,
            Uuid = Guid.NewGuid(),
            ScheduleId = scheduleId,
            Status = status,
            SessionDate = sessionDate ?? _timeZoneProvider.GetLocalNow().Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = CreateRowVersion(),
            Schedule = CreateTestSchedule(scheduleId, "Monday")
        };
    }

    private Schedules CreateTestSchedule(int id, string dayOfWeek)
    {
        return new Schedules
        {
            Id = id,
            Uuid = Guid.NewGuid(),
            DayOfWeek = dayOfWeek,
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };
    }

    private static byte[] CreateRowVersion() => [1, 2, 3, 4];

    private Instructor CreateTestInstructor(int id, string userId)
    {
        return new Instructor
        {
            Id = id,
            Uuid = Guid.NewGuid(),
            UserId = userId,
            Firstname = "John",
            Lastname = "Doe"
        };
    }

    private Classroom CreateTestClassroom(int id)
    {
        return new Classroom
        {
            Id = id,
            Uuid = Guid.NewGuid(),
            Name = $"Room-{id}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateSessionAsync_UsesCurrentDate_WhenSessionDateIsNull()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var schedule = CreateTestSchedule(1, today.DayOfWeek.ToString());
        var request = new CreateSession
        {
            ScheduleId = schedule.Uuid,
            SessionDate = null, // This should default to today
            Description = "Test session with null date"
        };
        var createdSession = CreateTestSession(1, schedule.Id, sessionDate: today);

        _mockScheduleRepository
            .Setup(r => r.GetScheduleByUuidAsync(request.ScheduleId!.Value))
            .ReturnsAsync(schedule);

        _mockSessionRepository
            .Setup(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, today))
            .ReturnsAsync(false);

        _mockSessionRepository
            .Setup(r => r.CreateSessionAsync(It.IsAny<Session>()))
            .Callback<Session>(s =>
            {
                // Simulate database assigning an ID and setting timestamps
                createdSession.Status = s.Status;
                createdSession.ScheduleId = s.ScheduleId;
                createdSession.SessionDate = s.SessionDate;
                createdSession.Description = s.Description;
            })
            .ReturnsAsync(createdSession);

        _mockSessionRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockSessionRepository
            .Setup(r => r.GetSessionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(today, result.SessionDate.Date);
        _mockSessionRepository.Verify(r => r.SessionExistsForScheduleAndDateAsync(schedule.Id, today), Times.Once);
    }



    #endregion
}
