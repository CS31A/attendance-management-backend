using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services;

/// <summary>
/// Service implementation for Session-related business logic operations.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ILogger<SessionService> _logger;

    /// <summary>
    /// Initializes a new instance of the SessionService class.
    /// </summary>
    public SessionService(
        ISessionRepository sessionRepository,
        IClassroomRepository classroomRepository,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
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

    #region Helper Methods

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
