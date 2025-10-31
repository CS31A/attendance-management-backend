using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;

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
    /// Retrieves an attendance record by its ID with all navigation properties loaded, with change tracking enabled.
    /// Use for update operations where the entity needs to be tracked for changes.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <returns>The attendance record if found, null otherwise</returns>
    Task<AttendanceRecord?> GetByIdTrackedAsync(int id);

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
    /// Loads all navigation properties. Use GetBySessionIdForRosterAsync for roster views.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>Collection of attendance records for the session</returns>
    Task<List<AttendanceRecord>> GetBySessionIdAsync(int sessionId);

    /// <summary>
    /// Retrieves attendance records for a session optimized for roster display.
    /// Uses DTO projection with only essential fields (1 SQL query, no entity tracking).
    /// Recommended for roster views and listing pages.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>Collection of roster DTOs with student names and attendance status</returns>
    Task<List<SessionAttendanceRosterDto>> GetBySessionIdForRosterAsync(int sessionId);

    /// <summary>
    /// Retrieves a specific attendance record for a student in a session.
    /// Loads all navigation properties. Use GetBySessionAndStudentMinimalAsync for lookups.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="studentId">The student ID</param>
    /// <returns>The attendance record if found, null otherwise</returns>
    Task<AttendanceRecord?> GetBySessionAndStudentAsync(int sessionId, int studentId);

    /// <summary>
    /// Retrieves minimal attendance record data for a student in a session.
    /// Uses DTO projection without navigation properties (1 SQL query, optimized for lookups).
    /// Recommended for duplicate checks and simple existence verification.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="studentId">The student ID</param>
    /// <returns>Minimal attendance DTO if found, null otherwise</returns>
    Task<AttendanceMinimalDto?> GetBySessionAndStudentMinimalAsync(int sessionId, int studentId);

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
    /// Optimized: Uses AnyAsync without loading any data (fastest existence check).
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="sessionId">The session ID</param>
    /// <returns>True if an attendance record exists, false otherwise</returns>
    Task<bool> HasAttendanceRecordAsync(int studentId, int sessionId);

    /// <summary>
    /// Checks if a student has any attendance records.
    /// Optimized: Uses AnyAsync without loading any data.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <returns>True if the student has any attendance records, false otherwise</returns>
    Task<bool> HasAnyAttendanceAsync(int studentId);

    /// <summary>
    /// Checks if a session has any attendance records.
    /// Optimized: Uses AnyAsync without loading any data.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>True if the session has any attendance records, false otherwise</returns>
    Task<bool> SessionHasAttendanceAsync(int sessionId);

    /// <summary>
    /// Gets the count of attendance records for a student, optionally filtered by status.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="status">Optional status filter (Present, Late, Excused, Absent)</param>
    /// <returns>The count of matching attendance records</returns>
    Task<int> GetAttendanceCountAsync(int studentId, string? status = null);

    /// <summary>
    /// Retrieves all attendance records for listing with pagination support using DTO projection.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of records per page</param>
    /// <returns>Paginated collection of attendance record DTOs</returns>
    Task<List<AttendanceRecordResponseDto>> GetAllForListingAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves all attendance records for listing with optimized projection to lightweight DTO.
    /// Uses database projections for better performance - no entity tracking or split queries.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of records per page</param>
    /// <returns>Paginated collection of lightweight attendance list DTOs</returns>
    Task<List<AttendanceListDto>> GetAllForListingOptimizedAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves filtered attendance records with pagination using database-level filtering.
    /// Optimized: All filters applied at database level before loading data.
    /// </summary>
    /// <param name="studentId">Optional student ID filter</param>
    /// <param name="sessionId">Optional session ID filter</param>
    /// <param name="scheduleId">Optional schedule ID filter</param>
    /// <param name="sectionId">Optional section ID filter</param>
    /// <param name="subjectId">Optional subject ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter (inclusive)</param>
    /// <param name="endDate">Optional end date filter (inclusive)</param>
    /// <param name="isManualEntry">Optional manual entry filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Records per page</param>
    /// <returns>Tuple containing filtered records and total count</returns>
    Task<(List<AttendanceRecord> Records, int TotalCount)> GetFilteredAsync(
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
        int pageSize = 50);

    /// <summary>
    /// Gets attendance statistics without loading records into memory.
    /// Optimized: All calculations performed at database level using SQL aggregations.
    /// </summary>
    /// <param name="studentId">Optional student ID filter</param>
    /// <param name="sessionId">Optional session ID filter</param>
    /// <param name="scheduleId">Optional schedule ID filter</param>
    /// <param name="sectionId">Optional section ID filter</param>
    /// <param name="subjectId">Optional subject ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter (inclusive)</param>
    /// <param name="endDate">Optional end date filter (inclusive)</param>
    /// <param name="isManualEntry">Optional manual entry filter</param>
    /// <returns>Statistics tuple with counts and averages</returns>
    Task<(int Total, int Present, int Late, int Absent, int Excused, long AvgCheckInTicks)> GetStatisticsAsync(
        int? studentId = null,
        int? sessionId = null,
        int? scheduleId = null,
        int? sectionId = null,
        int? subjectId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isManualEntry = null);
}
