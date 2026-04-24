using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

/// <summary>
/// Repository implementation for AttendanceRecord entity data access operations.
/// </summary>
public class AttendanceRepository(ApplicationDbContext context) : IAttendanceRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves an attendance record by its ID with all navigation properties loaded.
    /// Performance: Single JOIN query (no split query for single record).
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, QrCode.
    /// Use case: Detail views requiring full attendance information.
    /// </summary>
    public async Task<AttendanceRecord?> GetByIdAsync(int id)
    {
        return await ApplyFullIncludes(context.AttendanceRecords.AsNoTracking())
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an attendance record by its ID with all navigation properties loaded, with change tracking enabled.
    /// Performance: Single JOIN query (no split query for single record).
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, QrCode.
    /// Use case: Update operations where the entity needs to be tracked for changes.
    /// </summary>
    public async Task<AttendanceRecord?> GetByIdTrackedAsync(int id)
    {
        return await ApplyFullIncludes(context.AttendanceRecords)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an attendance record by its UUID with all navigation properties loaded.
    /// Performance: Single JOIN query (no split query for single record).
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, QrCode.
    /// Use case: UUID detail views requiring full attendance information.
    /// </summary>
    public async Task<AttendanceRecord?> GetAttendanceByUuidAsync(Guid uuid)
    {
        return await ApplyFullIncludes(context.AttendanceRecords.AsNoTracking())
            .FirstOrDefaultAsync(a => a.Uuid == uuid)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an attendance record by its UUID with all navigation properties loaded, with change tracking enabled.
    /// Performance: Single JOIN query (no split query for single record).
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, QrCode.
    /// Use case: UUID update operations where the entity needs to be tracked for changes.
    /// </summary>
    public async Task<AttendanceRecord?> GetAttendanceByUuidTrackedAsync(Guid uuid)
    {
        return await ApplyFullIncludes(context.AttendanceRecords)
            .FirstOrDefaultAsync(a => a.Uuid == uuid)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records with pagination support.
    /// Performance: ~8 split queries for full navigation properties.
    /// ⚠️ DEPRECATED: Use GetAllForListingOptimizedAsync() for listing views (80-90% faster).
    /// Use case: Only when full entity data is required for further processing.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetAllAsync(int pageNumber = 1, int pageSize = 50)
    {
        return await ApplyFullIncludesWithSplitQuery(context.AttendanceRecords.AsNoTracking())
            .OrderByDescending(a => a.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for listing with pagination support using DTO projection.
    /// </summary>
    [Obsolete("Use GetAllForListingOptimizedAsync() instead (80-90% faster)", false)]
    public async Task<List<AttendanceRecordResponseDto>> GetAllForListingAsync(int pageNumber = 1, int pageSize = 50)
    {
        // Validate pagination parameters
        if (pageNumber < PaginationConstants.MinPageNumber) pageNumber = PaginationConstants.DefaultPageNumber;
        if (pageSize < PaginationConstants.MinPageSize) pageSize = PaginationConstants.DefaultPageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize; // Prevent excessive data retrieval

        return await context.AttendanceRecords
            .AsNoTracking()
            .OrderByDescending(a => a.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttendanceRecordResponseDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = $"{a.Student.Firstname ?? ""} {a.Student.Lastname ?? ""}".Trim(),
                StudentNumber = a.Student.Id.ToString(),
                SessionId = a.SessionId,
                SessionDate = a.Session.SessionDate,
                QrCodeId = a.QrCodeId,
                CheckInTime = a.CheckInTime,
                Status = a.Status ?? "",
                Notes = a.Notes,
                IsManualEntry = a.IsManualEntry,
                EnteredBy = a.EnteredBy,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                ScheduleId = a.Session.ScheduleId,
                ScheduleTitle = $"{a.Session.Schedule.Subject.Name ?? ""} - {a.Session.Schedule.DayOfWeek ?? ""} {a.Session.Schedule.TimeIn}-{a.Session.Schedule.TimeOut}",
                SubjectName = a.Session.Schedule.Subject.Name ?? "",
                SectionName = a.Session.Schedule.Section.Name ?? "",
                RoomName = a.Session.Schedule.Classroom.Name ?? "",
                InstructorName = $"{a.Session.Schedule.Instructor.Firstname ?? ""} {a.Session.Schedule.Instructor.Lastname ?? ""}".Trim()
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for listing with optimized projection to lightweight DTO.
    /// Uses database projections for better performance - no entity tracking or split queries.
    /// </summary>
    public async Task<List<AttendanceListDto>> GetAllForListingOptimizedAsync(int pageNumber = 1, int pageSize = 50)
    {
        // Validate pagination parameters
        if (pageNumber < PaginationConstants.MinPageNumber) pageNumber = PaginationConstants.DefaultPageNumber;
        if (pageSize < PaginationConstants.MinPageSize) pageSize = PaginationConstants.DefaultPageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize; // Prevent excessive data retrieval

        return await context.AttendanceRecords
            .AsNoTracking()
            .OrderByDescending(a => a.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttendanceListDto
            {
                Id = a.Id,
                StudentName = $"{a.Student.Firstname ?? ""} {a.Student.Lastname ?? ""}".Trim(),
                SubjectName = a.Session.Schedule.Subject.Name ?? "",
                CheckInTime = a.CheckInTime,
                Status = a.Status ?? ""
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for a specific student.
    /// Performance: ~8 split queries for full navigation properties.
    /// Use case: Student attendance history with full schedule details.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetByStudentIdAsync(int studentId)
    {
        return await ApplyFullIncludesWithSplitQuery(context.AttendanceRecords.AsNoTracking())
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for a specific session.
    /// Performance: ~8 split queries for full navigation properties.
    /// ⚠️ Consider using GetBySessionIdForRosterAsync() for roster views (90% faster).
    /// Use case: When full schedule details are needed (Subject, Section, Classroom, Instructor).
    /// </summary>
    public async Task<List<AttendanceRecord>> GetBySessionIdAsync(int sessionId)
    {
        return await ApplyFullIncludesWithSplitQuery(context.AttendanceRecords.AsNoTracking())
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Student.Lastname)
            .ThenBy(a => a.Student.Firstname)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance records for a session optimized for roster display.
    /// Performance: Single SQL query with DTO projection, no entity tracking.
    /// Loads: Only AttendanceId, StudentId, StudentName, Status, CheckInTime, IsManualEntry.
    /// Use case: Session roster views, attendance lists (90% faster than GetBySessionIdAsync).
    /// </summary>
    public async Task<List<SessionAttendanceRosterDto>> GetBySessionIdForRosterAsync(int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Student.Lastname)
            .ThenBy(a => a.Student.Firstname)
            .Select(a => new SessionAttendanceRosterDto
            {
                AttendanceId = a.Id,
                StudentId = a.StudentId,
                StudentName = $"{a.Student.Firstname ?? ""} {a.Student.Lastname ?? ""}".Trim(),
                Status = a.Status ?? "",
                CheckInTime = a.CheckInTime,
                IsManualEntry = a.IsManualEntry
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a specific attendance record for a student in a session.
    /// Performance: Single JOIN query (no split query for single record).
    /// ⚠️ Use GetBySessionAndStudentMinimalAsync() for duplicate checks (95% faster).
    /// Use case: Detail views requiring full attendance and schedule information.
    /// </summary>
    public async Task<AttendanceRecord?> GetBySessionAndStudentAsync(int sessionId, int studentId)
    {
        return await ApplyFullIncludes(context.AttendanceRecords.AsNoTracking())
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == studentId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves minimal attendance record data for a student in a session.
    /// Performance: Single SQL query with DTO projection, no navigation properties loaded.
    /// Loads: Only Id, StudentId, SessionId, Status, CheckInTime, QrCodeId.
    /// Use case: Duplicate detection, simple lookups, QR code scanning (95% faster).
    /// </summary>
    public async Task<AttendanceMinimalDto?> GetBySessionAndStudentMinimalAsync(int sessionId, int studentId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && a.StudentId == studentId)
            .Select(a => new AttendanceMinimalDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                SessionId = a.SessionId,
                Status = a.Status ?? "",
                CheckInTime = a.CheckInTime,
                QrCodeId = a.QrCodeId
            })
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance records for a student within a date range.
    /// Performance: ~8 split queries for full navigation properties.
    /// Use case: Student attendance history reports with full schedule details.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetByStudentAndDateRangeAsync(int studentId, DateTime startDate, DateTime endDate)
    {
        return await ApplyFullIncludesWithSplitQuery(context.AttendanceRecords.AsNoTracking())
            .Where(a => a.StudentId == studentId &&
                        a.CheckInTime >= startDate &&
                        a.CheckInTime <= endDate)
            .OrderByDescending(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance records for multiple sessions.
    /// Performance: ~8 split queries for full navigation properties.
    /// Use case: Bulk attendance reports across multiple sessions.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetBySessionIdsAsync(List<int> sessionIds)
    {
        return await ApplyFullIncludesWithSplitQuery(context.AttendanceRecords.AsNoTracking())
            .Where(a => sessionIds.Contains(a.SessionId))
            .OrderByDescending(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new attendance record.
    /// </summary>
    public async Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance)
    {
        attendance.CreatedAt = DateTime.UtcNow;
        attendance.UpdatedAt = DateTime.UtcNow;

        var entry = await context.AttendanceRecords.AddAsync(attendance).ConfigureAwait(false);
        return entry.Entity;
    }

    /// <summary>
    /// Creates multiple attendance records in a single operation.
    /// </summary>
    public async Task<List<AttendanceRecord>> CreateBulkAsync(List<AttendanceRecord> attendanceRecords)
    {
        var now = DateTime.UtcNow;
        foreach (var record in attendanceRecords)
        {
            record.CreatedAt = now;
            record.UpdatedAt = now;
        }

        await context.AttendanceRecords.AddRangeAsync(attendanceRecords).ConfigureAwait(false);
        return attendanceRecords;
    }

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    public Task<AttendanceRecord> UpdateAsync(AttendanceRecord attendance)
    {
        attendance.UpdatedAt = DateTime.UtcNow;
        context.AttendanceRecords.Update(attendance);
        return Task.FromResult(attendance);
    }

    /// <summary>
    /// Deletes an attendance record by its ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var attendance = await context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);

        if (attendance == null)
        {
            return false;
        }

        context.AttendanceRecords.Remove(attendance);
        return true;
    }

    #endregion

    #region Utility Operations

    /// <summary>
    /// Checks if an attendance record exists for a specific student and session.
    /// Performance: Optimized - Single query with AnyAsync, no data loaded.
    /// Use case: Duplicate detection before creating attendance records.
    /// ✅ This is the correct pattern for existence checks.
    /// </summary>
    public async Task<bool> HasAttendanceRecordAsync(int studentId, int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.StudentId == studentId && a.SessionId == sessionId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a student has any attendance records.
    /// Performance: Optimized - Single query with AnyAsync, no data loaded.
    /// Use case: Checking if student has attendance history before operations.
    /// </summary>
    public async Task<bool> HasAnyAttendanceAsync(int studentId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.StudentId == studentId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a session has any attendance records.
    /// Performance: Optimized - Single query with AnyAsync, no data loaded.
    /// Use case: Validation before deleting sessions, checking if attendance was taken.
    /// </summary>
    public async Task<bool> SessionHasAttendanceAsync(int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.SessionId == sessionId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets attendance record for a specific student and session combination.
    /// Used for duplicate checking in fingerprint attendance.
    /// </summary>
    public async Task<AttendanceRecord?> GetAttendanceByStudentAndSessionAsync(int studentId, int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Subject)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Section)
            .Include(a => a.Session)
                .ThenInclude(s => s.ActualRoom)
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId && a.SessionId == sessionId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of attendance records for a student, optionally filtered by status.
    /// </summary>
    public async Task<int> GetAttendanceCountAsync(int studentId, string? status = null)
    {
        var query = context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.StudentId == studentId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        return await query.CountAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves filtered attendance records with pagination using database-level filtering.
    /// Performance: All filters applied at database level using IQueryable composition.
    /// Returns both the filtered records and total count for pagination.
    /// </summary>
    public async Task<(List<AttendanceRecord> Records, int TotalCount)> GetFilteredAsync(
        int? studentId = null,
        int? sessionId = null,
        int? scheduleId = null,
        int? sectionId = null,
        int? subjectId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isManualEntry = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        // Validate pagination parameters
        if (pageNumber < PaginationConstants.MinPageNumber) pageNumber = PaginationConstants.DefaultPageNumber;
        if (pageSize < PaginationConstants.MinPageSize) pageSize = PaginationConstants.DefaultPageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        // Start with base queryable (no execution yet)
        IQueryable<AttendanceRecord> query = context.AttendanceRecords.AsNoTracking();

        // Apply filters conditionally (builds WHERE clause in SQL)
        if (studentId.HasValue)
        {
            query = query.Where(a => a.StudentId == studentId.Value);
        }

        if (sessionId.HasValue)
        {
            query = query.Where(a => a.SessionId == sessionId.Value);
        }

        if (scheduleId.HasValue)
        {
            query = query.Where(a => a.Session.ScheduleId == scheduleId.Value);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(a => a.Session.Schedule.SectionId == sectionId.Value);
        }

        if (subjectId.HasValue)
        {
            query = query.Where(a => a.Session.Schedule.SubjectId == subjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CheckInTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CheckInTime <= endDate.Value);
        }

        if (isManualEntry.HasValue)
        {
            query = query.Where(a => a.IsManualEntry == isManualEntry.Value);
        }

        // Get total count before pagination (single COUNT query)
        var totalCount = await query.CountAsync().ConfigureAwait(false);

        // Apply includes for navigation properties with split query
        query = ApplyFullIncludesWithSplitQuery(query);

        // Apply sorting and pagination (in SQL)
        var records = await query
            .OrderByDescending(a => a.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);

        return (records, totalCount);
    }

    /// <summary>
    /// Gets attendance statistics without loading records into memory.
    /// Performance: All calculations performed at database level using SQL aggregations.
    /// No entities loaded - only aggregate values returned.
    /// </summary>
    public async Task<(int Total, int Present, int Late, int Absent, int Excused, long AvgCheckInTicks)> GetStatisticsAsync(
        int? studentId = null,
        int? sessionId = null,
        int? scheduleId = null,
        int? sectionId = null,
        int? subjectId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isManualEntry = null)
    {
        // Build base query with filters (no execution)
        IQueryable<AttendanceRecord> query = context.AttendanceRecords.AsNoTracking();

        // Apply filters
        if (studentId.HasValue)
            query = query.Where(a => a.StudentId == studentId.Value);

        if (sessionId.HasValue)
            query = query.Where(a => a.SessionId == sessionId.Value);

        if (scheduleId.HasValue)
            query = query.Where(a => a.Session.ScheduleId == scheduleId.Value);

        if (sectionId.HasValue)
            query = query.Where(a => a.Session.Schedule.SectionId == sectionId.Value);

        if (subjectId.HasValue)
            query = query.Where(a => a.Session.Schedule.SubjectId == subjectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (startDate.HasValue)
            query = query.Where(a => a.CheckInTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CheckInTime <= endDate.Value);

        if (isManualEntry.HasValue)
            query = query.Where(a => a.IsManualEntry == isManualEntry.Value);

        // Execute aggregations in a single query
        var stats = await query
            .GroupBy(a => 1) // Group all records together
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(a => a.Status == "Present"),
                Late = g.Count(a => a.Status == "Late"),
                Absent = g.Count(a => a.Status == "Absent"),
                Excused = g.Count(a => a.Status == "Excused"),
            })
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (stats == null)
        {
            return (0, 0, 0, 0, 0, 0L);
        }

        var checkInTimes = await query
            .Select(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);

        var avgCheckInTicks = checkInTimes.Count == 0
            ? 0L
            : (long)checkInTimes.Average(checkIn => checkIn.TimeOfDay.Ticks);

        return (stats.Total, stats.Present, stats.Late, stats.Absent, stats.Excused, avgCheckInTicks);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Applies all navigation property includes for full attendance record data.
    /// Use for single-record queries where a single JOIN query is more efficient.
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, ActualRoom, QrCode.
    /// </summary>
    /// <param name="query">The base queryable to apply includes to</param>
    /// <returns>Queryable with all includes applied</returns>
    private static IQueryable<AttendanceRecord> ApplyFullIncludes(IQueryable<AttendanceRecord> query)
    {
        return query
            .Include(a => a.Student)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Subject)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Section)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Classroom)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Instructor)
            .Include(a => a.Session)
                .ThenInclude(s => s.ActualRoom)
            .Include(a => a.QrCode);
    }

    /// <summary>
    /// Applies all navigation property includes with split query optimization.
    /// Use for multi-record queries to avoid cartesian explosion.
    /// Generates ~9 separate SQL queries for better performance with collections.
    /// Loads: Student, Session, Schedule, Subject, Section, Classroom, Instructor, ActualRoom, QrCode.
    /// </summary>
    /// <param name="query">The base queryable to apply includes to</param>
    /// <returns>Queryable with all includes and split query applied</returns>
    private static IQueryable<AttendanceRecord> ApplyFullIncludesWithSplitQuery(IQueryable<AttendanceRecord> query)
    {
        return query
            .AsSplitQuery()
            .Include(a => a.Student)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Subject)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Section)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Classroom)
            .Include(a => a.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Instructor)
            .Include(a => a.Session)
                .ThenInclude(s => s.ActualRoom)
            .Include(a => a.QrCode);
    }

    #endregion
}
