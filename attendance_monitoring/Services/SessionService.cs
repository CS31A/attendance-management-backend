using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;

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
    private readonly UserContextService _userContextService;
    private readonly IHttpContextAccessor _httpContextAccessor;
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
        UserContextService userContextService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
        _enrollmentRepository = enrollmentRepository ?? throw new ArgumentNullException(nameof(enrollmentRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    public async Task<SessionResponseDto?> GetSessionByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving session by ID: {SessionId}", id);

        try
        {
            var session = await _sessionRepository.GetSessionByIdAsync(id).ConfigureAwait(false);

            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", id);
                throw new EntityNotFoundException<int>("Session", id);
            }

            _logger.LogInformation("Successfully retrieved session with ID: {SessionId}", id);
            return MapToResponseDto(session);
        }
        catch (EntityNotFoundException<int>)
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

            _logger.LogInformation("Successfully retrieved {Count} sessions", sessionList.Count);
            return sessionList.Select(MapToResponseDto);
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
    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleIdAsync(int scheduleId)
    {
        _logger.LogInformation("Retrieving sessions for schedule ID: {ScheduleId}", scheduleId);

        try
        {
            var sessions = await _sessionRepository.GetSessionsByScheduleIdAsync(scheduleId).ConfigureAwait(false);
            var sessionList = sessions.ToList();

            _logger.LogInformation("Successfully retrieved {Count} sessions for schedule ID: {ScheduleId}",
                sessionList.Count, scheduleId);
            return sessionList.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions for schedule ID {ScheduleId}", scheduleId);
            throw new EntityServiceException("Session", $"GetSessionsByScheduleId: {scheduleId}",
                "An error occurred while retrieving sessions", ex);
        }
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

            _logger.LogInformation("Successfully retrieved {Count} sessions with status: {Status}",
                sessionList.Count, status);
            return sessionList.Select(MapToResponseDto);
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

            _logger.LogInformation("Successfully retrieved {Count} sessions for date: {Date:yyyy-MM-dd}",
                sessionList.Count, date);
            return sessionList.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sessions for date {Date:yyyy-MM-dd}", date);
            throw new EntityServiceException("Session", $"GetSessionsByDate: {date:yyyy-MM-dd}",
                "An error occurred while retrieving sessions", ex);
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates the actual room for a session.
    /// Only active sessions can have their room updated.
    /// </summary>
    public async Task<(SessionResponseDto?, string?)> UpdateSessionRoomAsync(int sessionId, UpdateSessionRoom updateRequest)
    {
        _logger.LogInformation("Updating room for session ID: {SessionId} to classroom ID: {ClassroomId}",
            sessionId, updateRequest.ActualRoomId);

        try
        {
            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<int>("Session", sessionId);
            }

            // Validate session status - only active sessions can have room changes
            if (session.Status != "active")
            {
                var errorMessage = session.Status switch
                {
                    "not_started" => "Cannot update room for a session that has not started. Please start the session first.",
                    "ended" => "Cannot update room for a session that has already ended.",
                    "cancelled" => "Cannot update room for a cancelled session.",
                    _ => $"Cannot update room for a session with status: {session.Status}. Only active sessions can have room changes."
                };

                _logger.LogWarning("Session room update failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Validate that the new classroom exists
            var classroom = await _classroomRepository.GetClassroomByIdAsync(updateRequest.ActualRoomId)
                .ConfigureAwait(false);

            if (classroom == null)
            {
                var errorMessage = $"Classroom with ID {updateRequest.ActualRoomId} not found.";
                _logger.LogWarning("Session room update failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Update the session's actual room
            session.ActualRoomId = updateRequest.ActualRoomId;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated room for session ID: {SessionId} to classroom: {ClassroomName}",
                sessionId, classroom.Name);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return (updatedSession != null ? MapToResponseDto(updatedSession) : null, null);
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating room for session ID {SessionId}", sessionId);
            throw new EntityServiceException("Session", $"UpdateSessionRoom: {sessionId}",
                "An error occurred while updating the session room", ex);
        }
    }

    #endregion

    #region Lifecycle Management Operations

    /// <summary>
    /// Creates a new session for a schedule.
    /// </summary>
    public async Task<(SessionResponseDto?, string?)> CreateSessionAsync(CreateSession request)
    {
        // Use provided date or default to current date
        var effectiveSessionDate = request.SessionDate ?? DateTime.UtcNow.Date;
        
        _logger.LogInformation("Creating session for schedule ID: {ScheduleId} on date: {SessionDate:yyyy-MM-dd}",
            request.ScheduleId, effectiveSessionDate);

        try
        {
            // Validate that the schedule exists
            var schedule = await _scheduleRepository.GetScheduleByIdAsync(request.ScheduleId).ConfigureAwait(false);
            if (schedule == null)
            {
                var errorMessage = $"Schedule with ID {request.ScheduleId} not found.";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Check if a session already exists for this schedule on this date
            var sessionExists = await _sessionRepository.SessionExistsForScheduleAndDateAsync(
                request.ScheduleId, effectiveSessionDate).ConfigureAwait(false);

            if (sessionExists)
            {
                var errorMessage = $"A session already exists for schedule ID {request.ScheduleId} on {effectiveSessionDate:yyyy-MM-dd}.";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Validate that the session date matches the schedule's day of week
            var sessionDayOfWeek = effectiveSessionDate.DayOfWeek.ToString();
            if (!schedule.DayOfWeek.Equals(sessionDayOfWeek, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = $"Session date {effectiveSessionDate:yyyy-MM-dd} ({sessionDayOfWeek}) does not match the schedule's day of week ({schedule.DayOfWeek}).";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Validate that the session date is not in the past
            if (effectiveSessionDate.Date < DateTime.UtcNow.Date)
            {
                var errorMessage = "Cannot create a session for a past date.";
                _logger.LogWarning("Session creation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Create the session entity
            var session = new Session
            {
                ScheduleId = request.ScheduleId,
                SessionDate = effectiveSessionDate.Date,
                Status = "not_started",
                Description = request.Description
            };

            await _sessionRepository.CreateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created session ID: {SessionId} for schedule ID: {ScheduleId}",
                session.Id, request.ScheduleId);

            // Retrieve created session with navigation properties
            var createdSession = await _sessionRepository.GetSessionByIdAsync(session.Id).ConfigureAwait(false);

            return (createdSession != null ? MapToResponseDto(createdSession) : null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating session for schedule ID {ScheduleId}", request.ScheduleId);
            throw new EntityServiceException("Session", $"CreateSession: ScheduleId {request.ScheduleId}",
                "An error occurred while creating the session", ex);
        }
    }

    /// <summary>
    /// Starts a session, marking it as active.
    /// </summary>
    public async Task<(SessionResponseDto?, string?)> StartSessionAsync(int sessionId, StartSession request)
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
                return (null, errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<int>("Session", sessionId);
            }

            // Validate that the instructor is authorized to start this session
            if (session.Schedule?.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to start this session. Only the assigned instructor can start it.";
                _logger.LogWarning("Session start failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule?.InstructorId);
                return (null, errorMessage);
            }

            // Validate session status - only not_started sessions can be started
            if (session.Status != "not_started")
            {
                var errorMessage = session.Status switch
                {
                    "active" => "This session has already been started.",
                    "ended" => "Cannot start a session that has already ended.",
                    "cancelled" => "Cannot start a cancelled session.",
                    _ => $"Cannot start a session with status: {session.Status}."
                };
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Validate that the session date is today
            if (session.SessionDate.Date != DateTime.UtcNow.Date)
            {
                var errorMessage = $"Cannot start session. The session is scheduled for {session.SessionDate:yyyy-MM-dd}, but today is {DateTime.UtcNow:yyyy-MM-dd}.";
                _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // If actualRoomId is provided, validate that the classroom exists
            int? actualRoomId = request.ActualRoomId ?? session.Schedule?.ClassroomId;
            if (actualRoomId.HasValue)
            {
                var classroom = await _classroomRepository.GetClassroomByIdAsync(actualRoomId.Value).ConfigureAwait(false);
                if (classroom == null)
                {
                    var errorMessage = $"Classroom with ID {actualRoomId.Value} not found.";
                    _logger.LogWarning("Session start failed: {ErrorMessage}", errorMessage);
                    return (null, errorMessage);
                }
            }

            // Calculate attendance cutoff time
            var attendanceCutoffMinutes = request.AttendanceCutoffMinutes ?? 15;
            var actualStartTime = DateTime.UtcNow;
            var attendanceCutoff = actualStartTime.AddMinutes(attendanceCutoffMinutes);

            // Update the session
            session.Status = "active";
            session.ActualStartTime = actualStartTime;
            session.ActualRoomId = actualRoomId;
            session.AttendanceCutOff = attendanceCutoff;
            session.StartedBy = instructor.Id;

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully started session ID: {SessionId} by instructor ID: {InstructorId}",
                sessionId, instructor.Id);

            // Send real-time notification to enrolled students
            var enrolledStudentIds = await GetEnrolledStudentIdsAsync(sessionId).ConfigureAwait(false);
            await _notificationService.NotifySessionStartedAsync(sessionId, enrolledStudentIds).ConfigureAwait(false);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return (updatedSession != null ? MapToResponseDto(updatedSession) : null, null);
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting session ID {SessionId}", sessionId);
            throw new EntityServiceException("Session", $"StartSession: {sessionId}",
                "An error occurred while starting the session", ex);
        }
    }

    /// <summary>
    /// Ends an active session.
    /// </summary>
    public async Task<(SessionResponseDto?, string?)> EndSessionAsync(int sessionId, EndSession request)
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
                return (null, errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<int>("Session", sessionId);
            }

            // Validate that the instructor is authorized to end this session
            if (session.Schedule?.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to end this session. Only the assigned instructor can end it.";
                _logger.LogWarning("Session end failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule?.InstructorId);
                return (null, errorMessage);
            }

            // Validate session status - only active sessions can be ended
            if (session.Status != "active")
            {
                var errorMessage = session.Status switch
                {
                    "not_started" => "Cannot end a session that has not been started. Please start the session first.",
                    "ended" => "This session has already been ended.",
                    "cancelled" => "Cannot end a cancelled session.",
                    _ => $"Cannot end a session with status: {session.Status}."
                };
                _logger.LogWarning("Session end failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Update the session
            session.Status = "ended";
            session.ActualEndTime = DateTime.UtcNow;
            session.EndedBy = instructor.Id;

            // Update description if provided
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                session.Description = string.IsNullOrWhiteSpace(session.Description)
                    ? request.Description
                    : $"{session.Description}\n\nEnd Notes: {request.Description}";
            }

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully ended session ID: {SessionId} by instructor ID: {InstructorId}",
                sessionId, instructor.Id);

            // Send real-time notification to enrolled students
            var enrolledStudentIds = await GetEnrolledStudentIdsAsync(sessionId).ConfigureAwait(false);
            await _notificationService.NotifySessionEndedAsync(sessionId, enrolledStudentIds).ConfigureAwait(false);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return (updatedSession != null ? MapToResponseDto(updatedSession) : null, null);
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while ending session ID {SessionId}", sessionId);
            throw new EntityServiceException("Session", $"EndSession: {sessionId}",
                "An error occurred while ending the session", ex);
        }
    }

    /// <summary>
    /// Cancels a session that has not started yet.
    /// </summary>
    public async Task<(SessionResponseDto?, string?)> CancelSessionAsync(int sessionId, CancelSession request)
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
                return (null, errorMessage);
            }

            var userId = await _userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                var errorMessage = "User ID not found in context.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                var errorMessage = "Instructor profile not found for the current user.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Retrieve the session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
            if (session == null)
            {
                _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
                throw new EntityNotFoundException<int>("Session", sessionId);
            }

            // Validate that the instructor is authorized to cancel this session
            if (session.Schedule?.InstructorId != instructor.Id)
            {
                var errorMessage = "You are not authorized to cancel this session. Only the assigned instructor can cancel it.";
                _logger.LogWarning("Session cancellation failed: {ErrorMessage} - Instructor ID: {InstructorId}, Schedule Instructor ID: {ScheduleInstructorId}",
                    errorMessage, instructor.Id, session.Schedule?.InstructorId);
                return (null, errorMessage);
            }

            // Validate session status - only not_started sessions can be cancelled
            if (session.Status != "not_started")
            {
                var errorMessage = session.Status switch
                {
                    "active" => "Cannot cancel an active session. Please end the session instead.",
                    "ended" => "Cannot cancel a session that has already ended.",
                    "cancelled" => "This session has already been cancelled.",
                    _ => $"Cannot cancel a session with status: {session.Status}."
                };
                _logger.LogWarning("Session cancellation failed: {ErrorMessage}", errorMessage);
                return (null, errorMessage);
            }

            // Update the session
            session.Status = "cancelled";
            session.Description = string.IsNullOrWhiteSpace(session.Description)
                ? $"Cancelled: {request.Reason}"
                : $"{session.Description}\n\nCancelled: {request.Reason}";

            await _sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await _sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully cancelled session ID: {SessionId} by instructor ID: {InstructorId}. Reason: {Reason}",
                sessionId, instructor.Id, request.Reason);

            // Retrieve updated session with navigation properties
            var updatedSession = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

            return (updatedSession != null ? MapToResponseDto(updatedSession) : null, null);
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cancelling session ID {SessionId}", sessionId);
            throw new EntityServiceException("Session", $"CancelSession: {sessionId}",
                "An error occurred while cancelling the session", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Retrieves the user IDs of all students enrolled in the session's section and subject.
    /// </summary>
    private async Task<IEnumerable<string>> GetEnrolledStudentIdsAsync(int sessionId)
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
            ScheduleId = session.ScheduleId,
            Status = session.Status,
            SessionDate = session.SessionDate,
            ActualStartTime = session.ActualStartTime,
            ActualEndTime = session.ActualEndTime,
            AttendanceCutOff = session.AttendanceCutOff,
            Description = session.Description,
            ActualRoomId = session.ActualRoomId,
            ActualRoomName = session.ActualRoom?.Name,
            StartedBy = session.StartedBy,
            StartedByName = session.InstructorWhoStarted != null
                ? $"{session.InstructorWhoStarted.Firstname} {session.InstructorWhoStarted.Lastname}"
                : null,
            EndedBy = session.EndedBy,
            EndedByName = session.InstructorWhoEnded != null
                ? $"{session.InstructorWhoEnded.Firstname} {session.InstructorWhoEnded.Lastname}"
                : null,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            // Schedule information
            SubjectCode = session.Schedule?.Subject?.Code,
            SubjectName = session.Schedule?.Subject?.Name,
            SectionName = session.Schedule?.Section?.Name,
            ScheduledRoomName = session.Schedule?.Classroom?.Name
        };
    }

    #endregion
}
