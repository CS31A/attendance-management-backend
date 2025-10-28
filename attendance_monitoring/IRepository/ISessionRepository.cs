using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Repository interface for Session entity data access operations.
/// </summary>
public interface ISessionRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves a session by its ID with all navigation properties loaded.
    /// </summary>
    /// <param name="id">The session ID</param>
    /// <returns>The session if found, null otherwise</returns>
    Task<Session?> GetSessionByIdAsync(int id);

    /// <summary>
    /// Retrieves all sessions with pagination support.
    /// </summary>
    /// <returns>Collection of all sessions</returns>
    Task<IEnumerable<Session>> GetAllSessionsAsync();

    /// <summary>
    /// Retrieves sessions for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <returns>Collection of sessions for the schedule</returns>
    Task<IEnumerable<Session>> GetSessionsByScheduleIdAsync(int scheduleId);

    /// <summary>
    /// Retrieves sessions by status.
    /// </summary>
    /// <param name="status">The session status (not_started, active, ended, cancelled)</param>
    /// <returns>Collection of sessions with the specified status</returns>
    Task<IEnumerable<Session>> GetSessionsByStatusAsync(string status);

    /// <summary>
    /// Retrieves sessions for a specific date.
    /// </summary>
    /// <param name="date">The session date</param>
    /// <returns>Collection of sessions on the specified date</returns>
    Task<IEnumerable<Session>> GetSessionsByDateAsync(DateTime date);

    /// <summary>
    /// Updates an existing session.
    /// </summary>
    /// <param name="session">The session entity to update</param>
    /// <returns>The updated session</returns>
    Task<Session> UpdateSessionAsync(Session session);

    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="session">The session entity to create</param>
    /// <returns>The created session</returns>
    Task<Session> CreateSessionAsync(Session session);
}
