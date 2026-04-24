using System.Security.Claims;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for attendance-related business logic operations.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Creates a new attendance record manually.
    /// </summary>
    /// <param name="request">The create attendance request</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>The created attendance record DTO</returns>
    Task<AttendanceRecordResponseDto> CreateAttendanceAsync(CreateAttendanceRequest request, ClaimsPrincipal user);

    /// <summary>
    /// Creates an attendance record from a QR code scan.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="sessionId">The session ID</param>
    /// <param name="qrCodeId">The QR code ID</param>
    /// <param name="checkInTime">The check-in time</param>
    /// <returns>The created attendance record DTO</returns>
    Task<AttendanceRecordResponseDto> CreateAttendanceFromQrScanAsync(int studentId, int sessionId, int qrCodeId, DateTime checkInTime);

    /// <summary>
    /// Retrieves an attendance record by its ID.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>The attendance record DTO if found and authorized</returns>
    Task<AttendanceRecordResponseDto?> GetAttendanceByIdAsync(int id, ClaimsPrincipal user);

    /// <summary>
    /// Retrieves an attendance record by its UUID.
    /// </summary>
    /// <param name="uuid">The attendance record UUID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>The attendance record DTO if found and authorized</returns>
    Task<AttendanceRecordResponseDto?> GetAttendanceByUuidAsync(Guid uuid, ClaimsPrincipal user);

    /// <summary>
    /// Retrieves all attendance records with filtering and pagination.
    /// </summary>
    /// <param name="filter">The filter and pagination parameters</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>Paged result of attendance records</returns>
    Task<PagedResult<AttendanceRecordResponseDto>> GetAllAttendanceAsync(AttendanceFilterRequest filter, ClaimsPrincipal user);

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <param name="request">The update request</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>The updated attendance record DTO</returns>
    Task<AttendanceRecordResponseDto> UpdateAttendanceAsync(int id, UpdateAttendanceRequest request, ClaimsPrincipal user);

    /// <summary>
    /// Updates an existing attendance record by its UUID.
    /// </summary>
    /// <param name="uuid">The attendance record UUID</param>
    /// <param name="request">The update request</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>The updated attendance record DTO</returns>
    Task<AttendanceRecordResponseDto> UpdateAttendanceByUuidAsync(Guid uuid, UpdateAttendanceRequest request, ClaimsPrincipal user);

    /// <summary>
    /// Deletes an attendance record.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAttendanceAsync(int id, ClaimsPrincipal user);

    /// <summary>
    /// Deletes an attendance record by its UUID.
    /// </summary>
    /// <param name="uuid">The attendance record UUID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAttendanceByUuidAsync(Guid uuid, ClaimsPrincipal user);

    /// <summary>
    /// Retrieves attendance history for a specific student.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>Student attendance history with statistics</returns>
    Task<StudentAttendanceHistoryDto> GetStudentAttendanceHistoryAsync(int studentId, ClaimsPrincipal user);

    /// <summary>
    /// Retrieves attendance information for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>Session attendance overview</returns>
    Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, ClaimsPrincipal user);

    /// <summary>
    /// Retrieves attendance summary statistics.
    /// </summary>
    /// <param name="filter">The filter parameters</param>
    /// <param name="user">The current user principal for authorization</param>
    /// <returns>Attendance summary statistics</returns>
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(AttendanceFilterRequest filter, ClaimsPrincipal user);

    /// <summary>
    /// Checks if a student can mark attendance for a session.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="sessionId">The session ID</param>
    /// <returns>True if attendance can be marked</returns>
    Task<bool> CanMarkAttendanceAsync(int studentId, int sessionId);

    /// <summary>
    /// Determines the attendance status based on check-in time.
    /// </summary>
    /// <param name="checkInTime">The check-in time</param>
    /// <param name="sessionStartTime">The session start time</param>
    /// <param name="lateCutoffMinutes">Minutes after start time to consider late (default 15)</param>
    /// <returns>The attendance status (Present or Late)</returns>
    string DetermineAttendanceStatus(DateTime checkInTime, DateTime sessionStartTime, int lateCutoffMinutes = 15);
}
