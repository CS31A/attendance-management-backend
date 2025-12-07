using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

/// <summary>
/// Repository implementation for Session entity data access operations.
/// </summary>
public class SessionRepository(ApplicationDbContext context) : ISessionRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves a session by its ID with all navigation properties loaded.
    /// </summary>
    public async Task<Session?> GetSessionByIdAsync(int id)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery() // Optimize performance for multiple Include chains
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .FirstOrDefaultAsync(s => s.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all sessions with navigation properties loaded.
    /// </summary>
    public async Task<IEnumerable<Session>> GetAllSessionsAsync()
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .OrderByDescending(s => s.SessionDate)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves sessions for a specific schedule.
    /// </summary>
    public async Task<IEnumerable<Session>> GetSessionsByScheduleIdAsync(int scheduleId)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .Where(s => s.ScheduleId == scheduleId)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves sessions by status.
    /// </summary>
    public async Task<IEnumerable<Session>> GetSessionsByStatusAsync(string status)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves sessions for a specific date.
    /// </summary>
    public async Task<IEnumerable<Session>> GetSessionsByDateAsync(DateTime date)
    {
        var dateOnly = date.Date;

        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .Where(s => s.SessionDate.Date == dateOnly)
            .OrderBy(s => s.Schedule.DayOfWeek)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new session.
    /// </summary>
    public async Task<Session> CreateSessionAsync(Session session)
    {
        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        var entry = await context.Sessions.AddAsync(session).ConfigureAwait(false);
        return entry.Entity;
    }

    /// <summary>
    /// Updates an existing session.
    /// </summary>
    public async Task<Session> UpdateSessionAsync(Session session)
    {
        // Load the session from database with tracking (without navigation properties)
        // This avoids conflicts with already-tracked related entities
        var trackedSession = await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == session.Id)
            .ConfigureAwait(false);

        if (trackedSession == null)
        {
            throw new InvalidOperationException($"Session with ID {session.Id} not found.");
        }

        // Update only the scalar properties
        trackedSession.Status = session.Status;
        trackedSession.ActualStartTime = session.ActualStartTime;
        trackedSession.ActualEndTime = session.ActualEndTime;
        trackedSession.AttendanceCutOff = session.AttendanceCutOff;
        trackedSession.Description = session.Description;
        trackedSession.ActualRoomId = session.ActualRoomId;
        trackedSession.StartedBy = session.StartedBy;
        trackedSession.EndedBy = session.EndedBy;
        trackedSession.UpdatedAt = DateTime.UtcNow;

        return trackedSession;
    }

    #endregion

    #region Utility Operations

    /// <summary>
    /// Checks if a session exists for a specific schedule on a given date.
    /// </summary>
    public async Task<bool> SessionExistsForScheduleAndDateAsync(int scheduleId, DateTime sessionDate)
    {
        var dateOnly = sessionDate.Date;

        return await context.Sessions
            .AsNoTracking()
            .AnyAsync(s => s.ScheduleId == scheduleId && s.SessionDate.Date == dateOnly)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves active sessions for a specific instructor.
    /// </summary>
    public async Task<IEnumerable<Session>> GetActiveSessionsByInstructorIdAsync(int instructorId)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .Where(s => s.Status == "active" && s.Schedule.InstructorId == instructorId)
            .OrderBy(s => s.ActualStartTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all sessions for a specific instructor (all statuses).
    /// </summary>
    public async Task<IEnumerable<Session>> GetSessionsByInstructorIdAsync(int instructorId)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .Where(s => s.Schedule.InstructorId == instructorId)
            .OrderByDescending(s => s.SessionDate)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a session by its ID without tracking (for read-only operations).
    /// </summary>
    public async Task<Session?> GetSessionByIdNoTrackingAsync(int id)
    {
        return await context.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Subject)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Section)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Classroom)
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch.Instructor)
            .Include(s => s.ActualRoom)
            .Include(s => s.InstructorWhoStarted)
            .Include(s => s.InstructorWhoEnded)
            .FirstOrDefaultAsync(s => s.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }

    #endregion
}
