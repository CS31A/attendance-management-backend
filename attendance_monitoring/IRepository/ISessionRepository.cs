using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;

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
    /// Retrieves a session by its UUID with all navigation properties loaded.
    /// </summary>
    /// <param name="uuid">The session UUID</param>
    /// <returns>The session if found, null otherwise</returns>
    Task<Session?> GetSessionByUuidAsync(Guid uuid);

    /// <summary>
    /// Retrieves a session by its UUID with all navigation properties loaded and tracking enabled.
    /// </summary>
    /// <param name="uuid">The session UUID</param>
    /// <returns>The session if found, null otherwise</returns>
    Task<Session?> GetSessionByUuidTrackedAsync(Guid uuid);

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

    /// <summary>
    /// Checks if a session exists for a specific schedule on a given date.
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <param name="sessionDate">The session date</param>
    /// <returns>True if a session exists, false otherwise</returns>
    Task<bool> SessionExistsForScheduleAndDateAsync(int scheduleId, DateTime sessionDate);

    /// <summary>
    /// Retrieves active sessions for a specific instructor.
    /// </summary>
    /// <param name="instructorId">The instructor ID</param>
    /// <returns>Collection of active sessions for the instructor</returns>
    Task<IEnumerable<Session>> GetActiveSessionsByInstructorIdAsync(int instructorId);

    /// <summary>
    /// Retrieves all sessions for a specific instructor (all statuses).
    /// </summary>
    /// <param name="instructorId">The instructor ID</param>
    /// <returns>Collection of all sessions for the instructor</returns>
    Task<IEnumerable<Session>> GetSessionsByInstructorIdAsync(int instructorId);

    /// <summary>
    /// Retrieves projected report rows for all sessions in a section with aggregated attendance counts.
    /// </summary>
    /// <param name="sectionId">The section ID</param>
    /// <param name="startDate">Optional inclusive start date</param>
    /// <param name="endDate">Optional inclusive end date</param>
    /// <returns>Projected session report rows</returns>
    Task<List<SessionReportRowDto>> GetSectionSessionReportRowsAsync(int sectionId, DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Retrieves projected report rows for all sessions owned by an instructor with aggregated attendance counts.
    /// </summary>
    /// <param name="instructorId">The instructor ID</param>
    /// <param name="startDate">Optional inclusive start date</param>
    /// <param name="endDate">Optional inclusive end date</param>
    /// <returns>Projected session report rows</returns>
    Task<List<SessionReportRowDto>> GetInstructorSessionReportRowsAsync(int instructorId, DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Retrieves a session by its ID without tracking (for read-only operations).
    /// </summary>
    /// <param name="id">The session ID</param>
    /// <returns>The session if found, null otherwise</returns>
    Task<Session?> GetSessionByIdNoTrackingAsync(int id);
}
