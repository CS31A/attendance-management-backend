using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
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
    Task<SessionResponseDto?> GetSessionByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a session by its UUID.
    /// </summary>
    /// <param name="id">The session UUID</param>
    /// <returns>Session response DTO if found</returns>
    Task<SessionResponseDto?> GetSessionByUuidAsync(Guid id);

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
    Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleIdAsync(Guid scheduleId);

    /// <summary>
    /// Retrieves sessions for a specific schedule UUID.
    /// </summary>
    /// <param name="scheduleUuid">The schedule UUID</param>
    /// <returns>Collection of session response DTOs</returns>
    Task<IEnumerable<SessionResponseDto>> GetSessionsByScheduleUuidAsync(Guid scheduleUuid);

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
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session is not active</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> UpdateSessionRoomAsync(Guid sessionId, UpdateSessionRoom updateRequest);

    /// <summary>
    /// Updates the actual room for a session by its UUID.
    /// Only active sessions can have their room updated.
    /// </summary>
    /// <param name="sessionUuid">The session UUID</param>
    /// <param name="updateRequest">The room update request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session is not active</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> UpdateSessionRoomByUuidAsync(Guid sessionUuid, UpdateSessionRoom updateRequest);

    /// <summary>
    /// Creates a new session for a schedule.
    /// </summary>
    /// <param name="request">The create session request</param>
    /// <returns>Created session response DTO</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when schedule or classroom is not found</exception>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> CreateSessionAsync(CreateSession request);

    /// <summary>
    /// Starts a session, marking it as active.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The start session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be started</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> StartSessionAsync(Guid sessionId, StartSession request);

    /// <summary>
    /// Starts a session, marking it as active, by its UUID.
    /// </summary>
    /// <param name="sessionUuid">The session UUID</param>
    /// <param name="request">The start session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be started</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> StartSessionByUuidAsync(Guid sessionUuid, StartSession request);

    /// <summary>
    /// Ends an active session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The end session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be ended</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> EndSessionAsync(Guid sessionId, EndSession request);

    /// <summary>
    /// Ends an active session by its UUID.
    /// </summary>
    /// <param name="sessionUuid">The session UUID</param>
    /// <param name="request">The end session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be ended</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> EndSessionByUuidAsync(Guid sessionUuid, EndSession request);

    /// <summary>
    /// Cancels a session that has not started yet.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The cancel session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be cancelled</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> CancelSessionAsync(Guid sessionId, CancelSession request);

    /// <summary>
    /// Cancels a session that has not started yet, by its UUID.
    /// </summary>
    /// <param name="sessionUuid">The session UUID</param>
    /// <param name="request">The cancel session request</param>
    /// <returns>Updated session response DTO</returns>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when session is not found</exception>
    /// <exception cref="ValidationException">Thrown when session cannot be cancelled</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<SessionResponseDto> CancelSessionByUuidAsync(Guid sessionUuid, CancelSession request);

    /// <summary>
    /// Retrieves all sessions belonging to the current instructor.
    /// </summary>
    /// <returns>Collection of session response DTOs for the current instructor</returns>
    /// <exception cref="EntityUnauthorizedException">Thrown when instructor context is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an unexpected error occurs</exception>
    Task<IEnumerable<SessionResponseDto>> GetMySessionsAsync();
}
