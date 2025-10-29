using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for Session-related business logic operations.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    /// <param name="id">The session ID</param>
    /// <returns>Session response DTO if found</returns>
    Task<SessionResponseDto?> GetSessionByIdAsync(int id);

    /// <summary>
    /// Retrieves all sessions.
    /// </summary>
    /// <returns>Collection of session response DTOs</returns>
    Task<IEnumerable<SessionResponseDto>> GetAllSessionsAsync();

    /// <summary>
    /// Retrieves sessions for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <returns>Collection of session response DTOs</returns>
    Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleIdAsync(int scheduleId);

    /// <summary>
    /// Retrieves sessions by status.
    /// </summary>
    /// <param name="status">The session status</param>
    /// <returns>Collection of session response DTOs</returns>
    Task<IEnumerable<SessionResponseDto>> GetSessionsByStatusAsync(string status);

    /// <summary>
    /// Retrieves sessions for a specific date.
    /// </summary>
    /// <param name="date">The session date</param>
    /// <returns>Collection of session response DTOs</returns>
    Task<IEnumerable<SessionResponseDto>> GetSessionsByDateAsync(DateTime date);

    /// <summary>
    /// Updates the actual room for a session.
    /// Only active sessions can have their room updated.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="updateRequest">The room update request</param>
    /// <returns>Tuple of updated session DTO and error message (if any)</returns>
    Task<(SessionResponseDto?, string?)> UpdateSessionRoomAsync(int sessionId, UpdateSessionRoom updateRequest);

    /// <summary>
    /// Creates a new session for a schedule.
    /// </summary>
    /// <param name="request">The create session request</param>
    /// <returns>Tuple of created session DTO and error message (if any)</returns>
    Task<(SessionResponseDto?, string?)> CreateSessionAsync(CreateSession request);

    /// <summary>
    /// Starts a session, marking it as active.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The start session request</param>
    /// <returns>Tuple of updated session DTO and error message (if any)</returns>
    Task<(SessionResponseDto?, string?)> StartSessionAsync(int sessionId, StartSession request);

    /// <summary>
    /// Ends an active session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The end session request</param>
    /// <returns>Tuple of updated session DTO and error message (if any)</returns>
    Task<(SessionResponseDto?, string?)> EndSessionAsync(int sessionId, EndSession request);

    /// <summary>
    /// Cancels a session that has not started yet.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The cancel session request</param>
    /// <returns>Tuple of updated session DTO and error message (if any)</returns>
    Task<(SessionResponseDto?, string?)> CancelSessionAsync(int sessionId, CancelSession request);
}
