using System.Security.Claims;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for report-specific data aggregation operations.
/// </summary>
public interface IReportsService
{
    /// <summary>
    /// Returns aggregate attendance summary statistics, respecting role-based access.
    /// </summary>
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(AttendanceFilterRequest filter, ClaimsPrincipal user);

    /// <summary>
    /// Returns attendance history and statistics for a specific student.
    /// </summary>
    Task<StudentAttendanceHistoryDto> GetStudentAttendanceReportAsync(int studentId, ClaimsPrincipal user);

    /// <summary>
    /// Returns full attendance overview for a specific session.
    /// </summary>
    Task<SessionAttendanceDto> GetSessionAttendanceReportAsync(int sessionId, ClaimsPrincipal user);

    /// <summary>
    /// Returns section-level attendance summary with per-session breakdown.
    /// </summary>
    Task<ClassAttendanceSummaryDto> GetClassAttendanceReportAsync(int sectionId, AttendanceFilterRequest filter, ClaimsPrincipal user);

    /// <summary>
    /// Returns session list with attendance counts for a specific instructor.
    /// </summary>
    Task<InstructorSessionsReportDto> GetInstructorSessionsReportAsync(int instructorId, AttendanceFilterRequest filter, ClaimsPrincipal user);

    /// <summary>
    /// Returns session list with attendance counts for a specific instructor UUID.
    /// </summary>
    Task<InstructorSessionsReportDto> GetInstructorSessionsReportAsync(Guid instructorUuid, AttendanceFilterRequest filter, ClaimsPrincipal user);
}
