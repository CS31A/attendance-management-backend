using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
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
    /// </summary>
    public async Task<AttendanceRecord?> GetByIdAsync(int id)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records with pagination support.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetAllAsync(int pageNumber = 1, int pageSize = 50)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .OrderByDescending(a => a.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for a specific student.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetByStudentIdAsync(int studentId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all attendance records for a specific session.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetBySessionIdAsync(int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Student.Lastname)
            .ThenBy(a => a.Student.Firstname)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a specific attendance record for a student in a session.
    /// </summary>
    public async Task<AttendanceRecord?> GetBySessionAndStudentAsync(int sessionId, int studentId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == studentId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance records for a student within a date range.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetByStudentAndDateRangeAsync(int studentId, DateTime startDate, DateTime endDate)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
            .Where(a => a.StudentId == studentId &&
                        a.CheckInTime >= startDate &&
                        a.CheckInTime <= endDate)
            .OrderByDescending(a => a.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance records for multiple sessions.
    /// </summary>
    public async Task<List<AttendanceRecord>> GetBySessionIdsAsync(List<int> sessionIds)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
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
            .Include(a => a.QrCode)
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
    /// </summary>
    public async Task<bool> HasAttendanceRecordAsync(int studentId, int sessionId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.StudentId == studentId && a.SessionId == sessionId)
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

    #endregion
}
