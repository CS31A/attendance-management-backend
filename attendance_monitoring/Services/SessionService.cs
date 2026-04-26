using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Service implementation for Session-related business logic operations.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly INotificationService _notificationService;
    private readonly IUserContextService _userContextService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAutomaticSessionEndService? _automaticSessionEndService;
    private readonly ConfiguredTimeZoneProvider _clock;
    private readonly ILogger<SessionService> _logger;

    /// <summary>
    /// Initializes a new instance of the SessionService class.
    /// </summary>
    public SessionService(
        ISessionRepository sessionRepository,
        IScheduleRepository scheduleRepository,
        IInstructorRepository instructorRepository,
        IClassroomRepository classroomRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        INotificationService notificationService,
        IUserContextService userContextService,
        IHttpContextAccessor httpContextAccessor,
        ConfiguredTimeZoneProvider clock,
        ILogger<SessionService> logger,
        IAutomaticSessionEndService? automaticSessionEndService = null)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
        _enrollmentRepository = enrollmentRepository ?? throw new ArgumentNullException(nameof(enrollmentRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _automaticSessionEndService = automaticSessionEndService;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    public async Task<SessionResponseDto?> GetSessionByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving session by ID: {SessionId}", id);

        try
        {
            var session = await _sessionRepository.GetSessionByIdAsync(id).ConfigureAwait(false);

            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", id);
                throw new EntityNotFoundException<Guid>("Session", id);
            }

            var normalizedSession = await NormalizeExpiredSessionAsync(session).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved session with ID: {SessionId}", id);
            return MapToResponseDto(normalizedSession);
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving session with ID {SessionId}", id);
            throw new EntityServiceException("Session", $"GetSessionById: {id}",
                "An error occurred while retrieving the session", ex);
        }
    }

    /// <summary>
    /// Retrieves all sessions.
    /// </summary>
    public async Task<IEnumerable<SessionResponseDto>> GetAllSessionsAsync()
    {
        _logger.LogInformation("Retrieving all sessions");

        try
        {
            var sessions = await _sessionRepository.GetAllSessionsAsync().ConfigureAwait(false);
            var sessionList = sessions.ToList();

            var normalizedSessions = await NormalizeExpiredSessionsAsync(sessionList).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {Count} sessions", sessionList.Count);
            return normalizedSessions.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all sessions");
            throw new EntityServiceException("Session", "GetAllSessions",
                "An error occurred while retrieving sessions", ex);
        }
    }

    /// <summary>
    /// Retrieves sessions for a specific schedule.
    /// </summary>
    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleIdAsync(Guid scheduleId)
    {
        _logger.LogInformation("Retrieving sessions for schedule ID: {ScheduleId}", scheduleId);

        try
        {
            var sessions = await _sessionRepository.GetSessionsByScheduleIdAsync(scheduleId).ConfigureAwait(false);
            var sessionList = sessions.ToList();

            var normalizedSessions = await NormalizeExpiredSessionsAsync(sessionList).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {Count} sessions for schedule ID: {ScheduleId}",
                sessionList.Count, scheduleId);
            return normalizedSessions.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions for schedule ID {ScheduleId}", scheduleId);
            throw new EntityServiceException("Session", $"GetSessionsByScheduleId: {scheduleId}",
                "An error occurred while retrieving sessions", ex);
        }
    }

    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleUuidAsync(Guid scheduleUuid)
    {
        var schedule = await _scheduleRepository.GetScheduleByUuidAsync(scheduleUuid).ConfigureAwait(false);
        if (schedule == null)
        {
            throw new EntityNotFoundException<Guid>("Schedule", scheduleUuid);
        }

        return await GetSessionsByScheduleIdAsync(schedule.Id).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves sessions by status.
    /// </summary>
    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByStatusAsync(string status)
    {
        _logger.LogInformation("Retrieving sessions with status: {Status}", status);

        try
        {
            var sessions = await _sessionRepository.GetSessionsByStatusAsync(status).ConfigureAwait(false);
            var sessionList = sessions.ToList();

            var normalizedSessions = await NormalizeExpiredSessionsAsync(sessionList).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {Count} sessions with status: {Status}",
                sessionList.Count, status);
            return normalizedSessions
                .Where(session => session.Status == status)
                .Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions with status {Status}", status);
            throw new EntityServiceException("Session", $"GetSessionsByStatus: {status}",
                "An error occurred while retrieving sessions", ex);
        }
    }

    /// <summary>
    /// Retrieves sessions for a specific date.
    /// </summary>
    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByDateAsync(DateTime date)
    {
        _logger.LogInformation("Retrieving sessions for date: {Date:yyyy-MM-dd}", date);

        try
        {
            var sessions = await _sessionRepository.GetSessionsByDateAsync(date).ConfigureAwait(false);
            var sessionList = sessions.ToList();

            var normalizedSessions = await NormalizeExpiredSessionsAsync(sessionList).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {Count} sessions for date: {Date:yyyy-MM-dd}",
                sessionList.Count, date);
            return normalizedSessions.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions for date {Date:yyyy-MM-dd}", date);
            throw new EntityServiceException("Session", $"GetSessionsByDate: {date:yyyy-MM-dd}",
                "An error occurred while retrieving sessions", ex);
        }
    }

    /// <summary>
    /// Retrieves all sessions belonging to the current instructor.
    /// </summary>
    public async Task<IEnumerable<SessionResponseDto>> GetMySessionsAsync()
    {
        _logger.LogInformation("Retrieving sessions for the current instructor");

        try
        {
            // Get current user context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                var errorMessage = "User context not found.";
                _logger.LogWarning("GetMySessions failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "GetMySessions", "unknown", errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("GetMySessions failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "GetMySessions", "unknown", errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("GetMySessions failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "GetMySessions", userId, errorMessage);
            }

            // Get all sessions for this instructor using the database-level filtered query
            var instructorSessions = await _sessionRepository.GetSessionsByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
            var sessionList = instructorSessions.ToList();

            _logger.LogInformation("Successfully retrieved {Count} sessions for instructor ID: {InstructorId}",
                sessionList.Count, instructor.Id);

            var normalizedSessions = await NormalizeExpiredSessionsAsync(sessionList).ConfigureAwait(false);

            return normalizedSessions.Select(MapToResponseDto);
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions for the current instructor");
            throw new EntityServiceException("Session", "GetMySessions",
                "An error occurred while retrieving your sessions", ex);
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates the actual room for a session.
    /// Only active sessions can have their room updated.
    /// </summary>
    public async Task<SessionResponseDto> UpdateSessionRoomAsync(Guid sessionId, UpdateSessionRoom updateRequest)
    {
        if (!updateRequest.ActualRoomId.HasValue || updateRequest.ActualRoomId.Value == Guid.Empty)
        {
            throw new ValidationException("ActualRoomId is required.");
        }

        var resolvedClassroom = await _classroomRepository.GetClassroomByUuidAsync(updateRequest.ActualRoomId.Value).ConfigureAwait(false);
        if (resolvedClassroom == null)
        {
            throw new EntityNotFoundException<Guid>("Classroom", updateRequest.ActualRoomId.Value);
        }

        _logger.LogInformation("Updating room for session ID: {SessionId} to classroom ID: {ClassroomId}",
            sessionId, updateRequest.ActualRoomId);

        try
        {
            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<Guid>("Session", sessionId);
            }

            session = await NormalizeExpiredSessionAsync(session).ConfigureAwait(false);

            // Validate session status - only active sessions can have room changes
            if (session.Status != SessionStatusConstants.Active)
            {
                var errorMessage = session.Status switch
                {
                    SessionStatusConstants.NotStarted => "Cannot update room for a session that has not started. Please start the session first.",
                    SessionStatusConstants.Ended => "Cannot update room for a session that has already ended.",
                    SessionStatusConstants.Cancelled => "Cannot update room for a cancelled session.",
                    _ => $"Cannot update room for a session with status: {session.Status}. Only active sessions can have room changes."
                };

                _logger.LogWarning("Session room update failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            var classroom = resolvedClassroom;

            // Update the session's actual room
            EnsureRowVersion(updateRequest.RowVersion, "update the room for");
            session.ActualRoomId = resolvedClassroom.Id;
            session.RowVersion = updateRequest.RowVersion!;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated room for session ID: {SessionId} to classroom: {ClassroomName}",
                sessionId, classroom.Name);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return updatedSession != null ? MapToResponseDto(updatedSession)
                : throw new EntityServiceException("Session", $"UpdateSessionRoom: {sessionId}",
                    "Failed to retrieve updated session");
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while updating room for session ID {SessionId}", sessionId);
            throw new EntityConflictException(
                "Session",
                "concurrent-update",
                "Session room could not be updated because the session was modified by another request.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating room for session ID {SessionId}", sessionId);
            throw ExceptionHandlingHelper.CreateServiceException("Session", "UpdateSessionRoom", ex);
        }
    }

    #endregion

    #region Lifecycle Management Operations

    /// <summary>
    /// Creates a new session for a schedule.
    /// </summary>
    public async Task<SessionResponseDto> CreateSessionAsync(CreateSession request)
    {
        if (!request.ScheduleId.HasValue || request.ScheduleId.Value == Guid.Empty)
        {
            throw new ValidationException("ScheduleId is required.");
        }

        var resolvedSchedule = await _scheduleRepository.GetScheduleByUuidAsync(request.ScheduleId.Value).ConfigureAwait(false);
        if (resolvedSchedule == null)
        {
            throw new EntityNotFoundException<Guid>("Schedule", request.ScheduleId.Value);
        }

        var scheduleEntityId = resolvedSchedule.Id;

        // Use provided date or default to current date (using local time for session scheduling)
        var today = _clock.GetLocalNow().Date;
        var effectiveSessionDate = request.SessionDate ?? today;

        _logger.LogInformation("Creating session for schedule ID: {ScheduleId} on date: {SessionDate:yyyy-MM-dd}",
            request.ScheduleId, effectiveSessionDate);

        try
        {
            // Validate that the schedule exists
            var schedule = resolvedSchedule;

            // Check if a session already exists for this schedule on this date
            var sessionExists = await _sessionRepository.SessionExistsForScheduleAndDateAsync(
                scheduleEntityId, effectiveSessionDate).ConfigureAwait(false);

            if (sessionExists)
            {
                var errorMessage = $"A session already exists for schedule ID {request.ScheduleId.Value} on {effectiveSessionDate:yyyy-MM-dd}.";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                throw new EntityAlreadyExistsException<Guid>("Session", "ScheduleId", request.ScheduleId.Value, errorMessage);
            }

            // Validate that the session date matches the schedule's day of week
            var sessionDayOfWeek = effectiveSessionDate.DayOfWeek.ToString();
            var scheduleMatchesDay = schedule.DayOfWeek.Equals(sessionDayOfWeek, StringComparison.OrdinalIgnoreCase);
            if (!scheduleMatchesDay)
            {
                if (!request.AllowOffScheduleDate)
                {
                    var errorMessage = $"Session date {effectiveSessionDate:yyyy-MM-dd} ({sessionDayOfWeek}) does not match the schedule's day of week ({schedule.DayOfWeek}).";
                    _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                    throw new ValidationException(errorMessage);
                }

                var trimmedReason = request.OffScheduleReason?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedReason))
                {
                    const string errorMessage = "Off-schedule reason is required when the session date does not match the schedule day.";
                    _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                    throw new ValidationException(errorMessage);
                }

                if (trimmedReason.Length < 5)
                {
                    const string errorMessage = "Off-schedule reason must be at least 5 characters.";
                    _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                    throw new ValidationException(errorMessage);
                }

                _logger.LogInformation(
                    "Allowing off-schedule session creation for schedule ID {ScheduleId} on {SessionDate:yyyy-MM-dd}. Reason: {OffScheduleReason}",
                    request.ScheduleId.Value,
                    effectiveSessionDate,
                    trimmedReason);
            }

            // Validate that the session date is not in the past (using local time)
            if (effectiveSessionDate.Date < today)
            {
                var errorMessage = "Cannot create a session for a past date.";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            // Create the session entity
            var session = new Session
            {
                ScheduleId = scheduleEntityId,
                SessionDate = effectiveSessionDate.Date,
                Status = SessionStatusConstants.NotStarted,
                Description = request.Description
            };

            await _sessionRepository.CreateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created session ID: {SessionId} for schedule ID: {ScheduleId}",
                session.Id, request.ScheduleId);

            // Retrieve created session with navigation properties
            var createdSession = await _sessionRepository.GetSessionByIdAsync(session.Id).ConfigureAwait(false);

            return createdSession != null ? MapToResponseDto(createdSession)
                : throw new EntityServiceException("Session", $"CreateSession: ScheduleId {request.ScheduleId}",
                    "Failed to retrieve created session");
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityAlreadyExistsException<Guid>)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating session for schedule ID {ScheduleId}", request.ScheduleId);
            throw ExceptionHandlingHelper.CreateServiceException("Session", "CreateSession", ex);
        }
    }

    /// <summary>
    /// Starts a session, marking it as active.
    /// </summary>
    public async Task<SessionResponseDto> StartSessionAsync(Guid sessionId, StartSession request)
    {
        _logger.LogInformation("Starting session ID: {SessionId}", sessionId);

        try
        {
            // Get current user context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                var errorMessage = "User context not found.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Start", "unknown", errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Start", "unknown", errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Start", userId, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<Guid>("Session", sessionId);
            }

            // Validate that the instructor is authorized to start this session
            if (session.Schedule.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to start this session. Only the assigned instructor can start it.";
                _logger.LogWarning("Session start failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule.InstructorId);
                throw new EntityUnauthorizedException("Session", "Start", userId, errorMessage);
            }

            // Validate session status - only not_started sessions can be started
            if (session.Status != SessionStatusConstants.NotStarted)
            {
                var errorMessage = session.Status switch
                {
                    SessionStatusConstants.Active => "This session has already been started.",
                    SessionStatusConstants.Ended => "Cannot start a session that has already ended.",
                    SessionStatusConstants.Cancelled => "Cannot start a cancelled session.",
                    _ => $"Cannot start a session with status: {session.Status}."
                };
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            // Validate that the session date is today (using local time)
            var today = _clock.GetLocalNow().Date;
            if (session.SessionDate.Date != today)
            {
                var errorMessage = $"Cannot start session. The session is scheduled for {session.SessionDate:yyyy-MM-dd}, but today is {today:yyyy-MM-dd}.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            Classroom? resolvedClassroom = null;
            if (request.ActualRoomId.HasValue)
            {
                resolvedClassroom = await _classroomRepository.GetClassroomByUuidAsync(request.ActualRoomId.Value).ConfigureAwait(false);
                if (resolvedClassroom == null)
                {
                    throw new EntityNotFoundException<Guid>("Classroom", request.ActualRoomId.Value);
                }
            }

            // If actualRoomId is provided, validate that the classroom exists
            var actualRoomId = resolvedClassroom?.Id ?? session.Schedule?.ClassroomId;
            if (actualRoomId.HasValue)
            {
                var classroom = resolvedClassroom ?? await _classroomRepository.GetClassroomByIdAsync(actualRoomId.Value).ConfigureAwait(false);
                if (classroom == null)
                {
                    var errorMessage = $"Classroom with ID {actualRoomId.Value} not found.";
                    _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                    throw new EntityNotFoundException<Guid>("Classroom", actualRoomId.Value);
                }
            }

            // Calculate attendance cutoff time (using local time)
            var attendanceCutoffMinutes = request.AttendanceCutoffMinutes ?? 15;
            var actualStartTime = _clock.GetLocalNow();
            var attendanceCutoff = actualStartTime.AddMinutes(attendanceCutoffMinutes);

            // Update the session
            EnsureRowVersion(request.RowVersion, "start");
            session.Status = SessionStatusConstants.Active;
            session.ActualStartTime = actualStartTime;
            session.ActualRoomId = actualRoomId;
            session.AttendanceCutOff = attendanceCutoff;
            session.StartedBy = instructor.Id;
            session.RowVersion = request.RowVersion!;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully started session ID: {SessionId} by instructor ID: {InstructorId}",
                sessionId, instructor.Id);

            // Send real-time notification to enrolled students
            var enrolledStudentIds = await GetEnrolledStudentIdsAsync(sessionId).ConfigureAwait(false);
            await _notificationService.NotifySessionStartedAsync(sessionId, enrolledStudentIds).ConfigureAwait(false);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return updatedSession != null ? MapToResponseDto(updatedSession)
                : throw new EntityServiceException("Session", $"StartSession: {sessionId}",
                    "Failed to retrieve updated session");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Session start conflict for session ID {SessionId}", sessionId);
            throw new EntityConflictException(
                "Session",
                "concurrent-start",
                "Session start could not be completed because the session was updated by another request.",
                ex);
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting session ID {SessionId}", sessionId);
            throw ExceptionHandlingHelper.CreateServiceException("Session", "StartSession", ex);
        }
    }

    /// <summary>
    /// Ends an active session.
    /// </summary>
    public async Task<SessionResponseDto> EndSessionAsync(Guid sessionId, EndSession request)
    {
        _logger.LogInformation("Ending session ID: {SessionId}", sessionId);

        try
        {
            // Get current user context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                var errorMessage = "User context not found.";
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "End", "unknown", errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "End", "unknown", errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "End", userId, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<Guid>("Session", sessionId);
            }

            // Validate that the instructor is authorized to end this session
            if (session.Schedule.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to end this session. Only the assigned instructor can end it.";
                _logger.LogWarning("Session end failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule.InstructorId);
                throw new EntityUnauthorizedException("Session", "End", userId, errorMessage);
            }

            session = await NormalizeExpiredSessionAsync(session).ConfigureAwait(false);

            // Validate session status - only active sessions can be ended
            if (session.Status != SessionStatusConstants.Active)
            {
                var errorMessage = session.Status switch
                {
                    SessionStatusConstants.NotStarted => "Cannot end a session that has not been started. Please start the session first.",
                    SessionStatusConstants.Ended => "This session has already been ended.",
                    SessionStatusConstants.Cancelled => "Cannot end a cancelled session.",
                    _ => $"Cannot end a session with status: {session.Status}."
                };
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            // Update the session
            session.Status = SessionStatusConstants.Ended;
            session.ActualEndTime = _clock.GetLocalNow();
            session.EndedBy = instructor.Id;

            // Update description if provided
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                session.Description = string.IsNullOrWhiteSpace(session.Description)
                    ? request.Description
                    : $"{session.Description}\n\nEnd Notes: {request.Description}";
            }
            EnsureRowVersion(request.RowVersion, "end");
            session.RowVersion = request.RowVersion!;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully ended session ID: {SessionId} by instructor ID: {InstructorId}",
                sessionId, instructor.Id);

            // Send real-time notification to enrolled students
            var enrolledStudentIds = await GetEnrolledStudentIdsAsync(sessionId).ConfigureAwait(false);
            await _notificationService.NotifySessionEndedAsync(sessionId, enrolledStudentIds).ConfigureAwait(false);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return updatedSession != null ? MapToResponseDto(updatedSession)
                : throw new EntityServiceException("Session", $"EndSession: {sessionId}",
                    "Failed to retrieve updated session");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for session ID {SessionId}", sessionId);
            throw new EntityConflictException(
                "Session",
                "concurrent-update",
                "Session could not be updated because it was modified by another request.",
                ex);
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while ending session ID {SessionId}", sessionId);
            throw ExceptionHandlingHelper.CreateServiceException("Session", "EndSession", ex);
        }
    }

    /// <summary>
    /// Cancels a session that has not started yet.
    /// </summary>
    public async Task<SessionResponseDto> CancelSessionAsync(Guid sessionId, CancelSession request)
    {
        _logger.LogInformation("Cancelling session ID: {SessionId}", sessionId);

        try
        {
            // Get current user context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                var errorMessage = "User context not found.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Cancel", "unknown", errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Cancel", "unknown", errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                throw new EntityUnauthorizedException("Session", "Cancel", userId, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<Guid>("Session", sessionId);
            }

            // Validate that the instructor is authorized to cancel this session
            if (session.Schedule.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to cancel this session. Only the assigned instructor can cancel it.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule.InstructorId);
                throw new EntityUnauthorizedException("Session", "Cancel", userId, errorMessage);
            }

            // Validate session status - only not_started sessions can be cancelled
            if (session.Status != SessionStatusConstants.NotStarted)
            {
                var errorMessage = session.Status switch
                {
                    SessionStatusConstants.Active => "Cannot cancel an active session. Please end the session instead.",
                    SessionStatusConstants.Ended => "Cannot cancel a session that has already ended.",
                    SessionStatusConstants.Cancelled => "This session has already been cancelled.",
                    _ => $"Cannot cancel a session with status: {session.Status}."
                };
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                throw new ValidationException(errorMessage);
            }

            // Update the session
            EnsureRowVersion(request.RowVersion, "cancel");
            session.Status = SessionStatusConstants.Cancelled;
            session.Description = string.IsNullOrWhiteSpace(session.Description)
                ? $"Cancelled: {request.Reason}"
                : $"{session.Description}\n\nCancelled: {request.Reason}";
            session.RowVersion = request.RowVersion!;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully cancelled session ID: {SessionId} by instructor ID: {InstructorId}. Reason: {Reason}",
                sessionId, instructor.Id, request.Reason);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return updatedSession != null ? MapToResponseDto(updatedSession)
                : throw new EntityServiceException("Session", $"CancelSession: {sessionId}",
                    "Failed to retrieve updated session");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for session ID {SessionId}", sessionId);
            throw new EntityConflictException(
                "Session",
                "concurrent-update",
                "Session could not be updated because it was modified by another request.",
                ex);
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cancelling session ID {SessionId}", sessionId);
            throw ExceptionHandlingHelper.CreateServiceException("Session", "CancelSession", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Retrieves the user IDs of all students enrolled in the session's section and subject.
    /// </summary>
    private async Task<IEnumerable<string>> GetEnrolledStudentIdsAsync(Guid sessionId)
    {
        try
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session?.Schedule == null)
            {
                _logger.LogWarning("Session {SessionId} or Schedule not found for enrollment lookup", sessionId);
                return Enumerable.Empty<string>();
            }

            var sectionId = session.Schedule.SectionId;
            var subjectId = session.Schedule.SubjectId;

            // Get all active enrollments for the section
            var enrollments = await _enrollmentRepository.GetActiveSectionEnrollmentsAsync(sectionId).ConfigureAwait(false);

            // Filter for the specific subject and extract user IDs
            return enrollments
                .Where(e => e.SubjectId == subjectId && e.Student?.UserId != null)
                .Select(e => e.Student.UserId!)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrolled student IDs for session {SessionId}", sessionId);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Maps a Session entity to a SessionResponseDto.
    /// </summary>
    private SessionResponseDto MapToResponseDto(Session session)
    {
        // Validate critical navigation properties are loaded
        if (session.Schedule == null)
        {
            _logger.LogWarning("Session {SessionId} missing Schedule navigation property in MapToResponseDto", session.Id);
        }

        return new SessionResponseDto
        {
            Id = session.Id,
            ScheduleId = session.Schedule?.Id ?? Guid.Empty,
            Status = session.Status,
            SessionDate = session.SessionDate,
            ActualStartTime = session.ActualStartTime,
            ActualEndTime = session.ActualEndTime,
            AttendanceCutOff = session.AttendanceCutOff,
            Description = session.Description,
            ActualRoomId = session.ActualRoom?.Id,
            ActualRoomName = session.ActualRoom?.Name,
            StartedById = session.InstructorWhoStarted?.Id,
            StartedByName = session.InstructorWhoStarted != null
                ? $"{session.InstructorWhoStarted.Firstname} {session.InstructorWhoStarted.Lastname}"
                : null,
            EndedById = session.InstructorWhoEnded?.Id,
            EndedByName = session.InstructorWhoEnded != null
                ? $"{session.InstructorWhoEnded.Firstname} {session.InstructorWhoEnded.Lastname}"
                : null,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            RowVersion = session.RowVersion,
            // Schedule information
            SubjectCode = session.Schedule?.Subject?.Code,
            SubjectName = session.Schedule?.Subject?.Name,
            SectionName = session.Schedule?.Section?.Name,
            ScheduledRoomName = session.Schedule?.Classroom?.Name
        };
    }

    private static void EnsureRowVersion(byte[]? rowVersion, string operation)
    {
        if (rowVersion is not { Length: > 0 })
        {
            throw new ValidationException($"A rowVersion is required to {operation} this session.");
        }
    }

    private async Task<Session> NormalizeExpiredSessionAsync(Session session)
    {
        return _automaticSessionEndService == null
            ? session
            : await _automaticSessionEndService.AutoEndIfExpiredAsync(session).ConfigureAwait(false);
    }

    private async Task<List<Session>> NormalizeExpiredSessionsAsync(IEnumerable<Session> sessions)
    {
        var normalized = new List<Session>();
        foreach (var session in sessions)
        {
            normalized.Add(await NormalizeExpiredSessionAsync(session).ConfigureAwait(false));
        }

        return normalized;
    }

    #endregion

    #region UUID Entrypoints

    public async Task<SessionResponseDto?> GetSessionByUuidAsync(Guid id)
    {
        var session = await _sessionRepository.GetSessionByUuidAsync(id).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", id);
        }
        var normalizedSession = await NormalizeExpiredSessionAsync(session).ConfigureAwait(false);
        return MapToResponseDto(normalizedSession);
    }

    public async Task<SessionResponseDto> StartSessionByUuidAsync(Guid sessionUuid, StartSession request)
    {
        var session = await _sessionRepository.GetSessionByUuidAsync(sessionUuid).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }
        return await StartSessionAsync(session.Id, request).ConfigureAwait(false);
    }

    public async Task<SessionResponseDto> EndSessionByUuidAsync(Guid sessionUuid, EndSession request)
    {
        var session = await _sessionRepository.GetSessionByUuidAsync(sessionUuid).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }
        return await EndSessionAsync(session.Id, request).ConfigureAwait(false);
    }

    public async Task<SessionResponseDto> CancelSessionByUuidAsync(Guid sessionUuid, CancelSession request)
    {
        var session = await _sessionRepository.GetSessionByUuidAsync(sessionUuid).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }
        return await CancelSessionAsync(session.Id, request).ConfigureAwait(false);
    }

    public async Task<SessionResponseDto> UpdateSessionRoomByUuidAsync(Guid sessionUuid, UpdateSessionRoom request)
    {
        var session = await _sessionRepository.GetSessionByUuidAsync(sessionUuid).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }
        return await UpdateSessionRoomAsync(session.Id, request).ConfigureAwait(false);
    }

    #endregion
}
