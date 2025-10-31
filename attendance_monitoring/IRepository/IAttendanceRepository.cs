using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Repository interface for AttendanceRecord entity data access operations.
/// </summary>
public interface IAttendanceRepository : ISaveableRepository
{
    /// <summary>
    /// Creates a new attendance record.
    /// </summary>
    /// <param name="attendance">The attendance record to create</param>
    /// <returns>The created attendance record</returns>
    Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance);

    /// <summary>
    /// Creates multiple attendance records in a single operation.
    /// </summary>
    /// <param name="attendanceRecords">The list of attendance records to create</param>
    /// <returns>The list of created attendance records</returns>
    Task<List<AttendanceRecord>> CreateBulkAsync(List<AttendanceRecord> attendanceRecords);

    /// <summary>
    /// Retrieves an attendance record by its ID with all navigation properties loaded.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <returns>The attendance record if found, null otherwise</returns>
    Task<AttendanceRecord?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all attendance records with pagination support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of records per page</param>
    /// <returns>Paginated collection of attendance records</returns>
    Task<List<AttendanceRecord>> GetAllAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves all attendance records for a specific student.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <returns>Collection of attendance records for the student</returns>
    Task<List<AttendanceRecord>> GetByStudentIdAsync(int studentId);

    /// <summary>
    /// Retrieves all attendance records for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>Collection of attendance records for the session</returns>
    Task<List<AttendanceRecord>> GetBySessionIdAsync(int sessionId);

    /// <summary>
    /// Retrieves a specific attendance record for a student in a session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="studentId">The student ID</param>
    /// <returns>The attendance record if found, null otherwise</returns>
    Task<AttendanceRecord?> GetBySessionAndStudentAsync(int sessionId, int studentId);

    /// <summary>
    /// Retrieves attendance records for a student within a date range.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <returns>Collection of attendance records within the date range</returns>
    Task<List<AttendanceRecord>> GetByStudentAndDateRangeAsync(int studentId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Retrieves attendance records for multiple sessions.
    /// </summary>
    /// <param name="sessionIds">The list of session IDs</param>
    /// <returns>Collection of attendance records for the specified sessions</returns>
    Task<List<AttendanceRecord>> GetBySessionIdsAsync(List<int> sessionIds);

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    /// <param name="attendance">The attendance record to update</param>
    /// <returns>The updated attendance record</returns>
    Task<AttendanceRecord> UpdateAsync(AttendanceRecord attendance);

    /// <summary>
    /// Deletes an attendance record by its ID.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Checks if an attendance record exists for a specific student and session.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="sessionId">The session ID</param>
    /// <returns>True if an attendance record exists, false otherwise</returns>
    Task<bool> HasAttendanceRecordAsync(int studentId, int sessionId);

    /// <summary>
    /// Gets the count of attendance records for a student, optionally filtered by status.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="status">Optional status filter (Present, Late, Excused, Absent)</param>
    /// <returns>The count of matching attendance records</returns>
    Task<int> GetAttendanceCountAsync(int studentId, string? status = null);
}
