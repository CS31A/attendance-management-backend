using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;
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
    /// Performance: Single JOIN query (no split query for single record retrieval).
    /// </summary>
    public async Task<Session?> GetSessionByIdAsync(int id)
    {
        return await context.Sessions
            .AsNoTracking()
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

        if (session.RowVersion is { Length: > 0 })
        {
            context.Entry(trackedSession)
                .Property(s => s.RowVersion)
                .OriginalValue = session.RowVersion;
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
            .Where(s => s.Status == SessionStatusConstants.Active && s.Schedule.InstructorId == instructorId)
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
    /// Retrieves projected report rows for all sessions in a section with aggregated attendance counts.
    /// </summary>
    public async Task<List<SessionReportRowDto>> GetSectionSessionReportRowsAsync(int sectionId, DateTime? startDate, DateTime? endDate)
    {
        var query =
            from session in context.Sessions.AsNoTracking()
            join schedule in context.Schedules.AsNoTracking() on session.ScheduleId equals schedule.Id
            join subject in context.Subjects.AsNoTracking() on schedule.SubjectId equals subject.Id
            join section in context.Sections.AsNoTracking() on schedule.SectionId equals section.Id
            join attendance in context.AttendanceRecords.AsNoTracking() on session.Id equals attendance.SessionId into attendanceGroup
            where schedule.SectionId == sectionId
            select new SessionReportRowDto
            {
                SessionId = session.Id,
                SessionDate = session.SessionDate,
                Status = session.Status,
                SubjectName = subject.Name ?? string.Empty,
                SectionName = section.Name ?? string.Empty,
                DayOfWeek = schedule.DayOfWeek ?? string.Empty,
                PresentCount = attendanceGroup.Count(record => record.Status == "Present"),
                LateCount = attendanceGroup.Count(record => record.Status == "Late"),
                AbsentCount = attendanceGroup.Count(record => record.Status == "Absent"),
                ExcusedCount = attendanceGroup.Count(record => record.Status == "Excused"),
                TotalRecords = attendanceGroup.Count(),
                TotalEnrolled = context.StudentEnrollments.Count(se => se.SectionId == section.Id && se.IsActive),
            };

        if (startDate.HasValue)
            query = query.Where(row => row.SessionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(row => row.SessionDate <= endDate.Value);

        return await query
            .OrderByDescending(row => row.SessionDate)
            .ThenByDescending(row => row.SessionId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves projected report rows for all sessions owned by an instructor with aggregated attendance counts.
    /// </summary>
    public async Task<List<SessionReportRowDto>> GetInstructorSessionReportRowsAsync(int instructorId, DateTime? startDate, DateTime? endDate)
    {
        var query =
            from session in context.Sessions.AsNoTracking()
            join schedule in context.Schedules.AsNoTracking() on session.ScheduleId equals schedule.Id
            join subject in context.Subjects.AsNoTracking() on schedule.SubjectId equals subject.Id
            join section in context.Sections.AsNoTracking() on schedule.SectionId equals section.Id
            join attendance in context.AttendanceRecords.AsNoTracking() on session.Id equals attendance.SessionId into attendanceGroup
            where schedule.InstructorId == instructorId
            select new SessionReportRowDto
            {
                SessionId = session.Id,
                SessionDate = session.SessionDate,
                Status = session.Status,
                SubjectName = subject.Name ?? string.Empty,
                SectionName = section.Name ?? string.Empty,
                DayOfWeek = schedule.DayOfWeek ?? string.Empty,
                PresentCount = attendanceGroup.Count(record => record.Status == "Present"),
                LateCount = attendanceGroup.Count(record => record.Status == "Late"),
                AbsentCount = attendanceGroup.Count(record => record.Status == "Absent"),
                ExcusedCount = attendanceGroup.Count(record => record.Status == "Excused"),
                TotalRecords = attendanceGroup.Count(),
                TotalEnrolled = context.StudentEnrollments.Count(se => se.SectionId == section.Id && se.IsActive),
            };

        if (startDate.HasValue)
            query = query.Where(row => row.SessionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(row => row.SessionDate <= endDate.Value);

        return await query
            .OrderByDescending(row => row.SessionDate)
            .ThenByDescending(row => row.SessionId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a session by its ID without tracking (for read-only operations).
    /// Performance: Single JOIN query (no split query for single record retrieval).
    /// </summary>
    public async Task<Session?> GetSessionByIdNoTrackingAsync(int id)
    {
        return await context.Sessions
            .AsNoTracking()
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
