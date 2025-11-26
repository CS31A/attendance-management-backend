using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing attendance record operations.
/// Handles CRUD operations for attendance tracking and reporting.
/// </summary>
[Authorize(Policy = "PrivilegedPolicy")]
[ApiController]
[Route("api/attendance")]
public class AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger) : ControllerBase
{
    #region Create Operations

    /// <summary>
    /// Create a new attendance record manually.
    /// Only admins and instructors can manually create attendance records.
    /// </summary>
    /// <param name="request">The create attendance request</param>
    /// <returns>The created attendance record</returns>
    /// <response code="201">Returns the created attendance record</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Student or session not found</response>
    /// <response code="409">Attendance record already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    public async Task<ActionResult<AttendanceRecordResponseDto>> CreateAttendance([FromBody] CreateAttendanceRequest request)
    {
        logger.LogInformation("Creating attendance record for StudentId: {StudentId}, SessionId: {SessionId}",
            request.StudentId, request.SessionId);

        try
        {
            var attendance = await attendanceService.CreateAttendanceAsync(request, User);
            logger.LogInformation("Successfully created attendance record with ID: {Id}", attendance.Id);
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Entity not found while creating attendance");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating attendance");
            return Conflict(new { message = ex.Message });
        }
    }

    #endregion

    #region Read Operations

    /// <summary>
    /// Get a specific attendance record by ID.
    /// Students can only view their own records. Instructors can view records for their sessions.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <returns>The requested attendance record</returns>
    /// <response code="200">Returns the attendance record</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Attendance record not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetAttendance(int id)
    {
        logger.LogInformation("Getting attendance record with ID: {Id}", id);

        try
        {
            var attendance = await attendanceService.GetAttendanceByIdAsync(id, User);
            logger.LogInformation("Successfully retrieved attendance record with ID: {Id}", id);
            return Ok(attendance);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Attendance record with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to attendance record {Id}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Get all attendance records with optional filtering and pagination.
    /// Students can only view their own records. Instructors can view records for their sessions.
    /// </summary>
    /// <param name="filter">Filter and pagination parameters</param>
    /// <returns>Paginated list of attendance records</returns>
    /// <response code="200">Returns the paginated list of attendance records</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AttendanceRecordResponseDto>>> GetAllAttendance([FromQuery] AttendanceFilterRequest filter)
    {
        logger.LogInformation("Getting all attendance records with filters");

        var result = await attendanceService.GetAllAttendanceAsync(filter, User);
        logger.LogInformation("Successfully retrieved {Count} attendance records", result.Items.Count);
        return Ok(result);
    }

    /// <summary>
    /// Get attendance history for a specific student.
    /// Students can only view their own history.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <returns>Student attendance history with statistics</returns>
    /// <response code="200">Returns the student attendance history</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Student not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("student/{studentId:int}")]
    public async Task<ActionResult<StudentAttendanceHistoryDto>> GetStudentAttendanceHistory(int studentId)
    {
        logger.LogInformation("Getting attendance history for StudentId: {StudentId}", studentId);

        try
        {
            var history = await attendanceService.GetStudentAttendanceHistoryAsync(studentId, User);
            logger.LogInformation("Successfully retrieved attendance history for StudentId: {StudentId}", studentId);
            return Ok(history);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Student with ID {StudentId} not found", studentId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to student {StudentId} attendance", studentId);
            return Forbid();
        }
    }

    /// <summary>
    /// Get attendance overview for a specific session.
    /// Instructors can only view their own session attendance.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>Session attendance overview with student list</returns>
    /// <response code="200">Returns the session attendance overview</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Session not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("session/{sessionId:int}")]
    public async Task<ActionResult<SessionAttendanceDto>> GetSessionAttendance(int sessionId)
    {
        logger.LogInformation("Getting session attendance for SessionId: {SessionId}", sessionId);

        try
        {
            var sessionAttendance = await attendanceService.GetSessionAttendanceAsync(sessionId, User);
            logger.LogInformation("Successfully retrieved session attendance for SessionId: {SessionId}", sessionId);
            return Ok(sessionAttendance);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Session with ID {SessionId} not found", sessionId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to session {SessionId} attendance", sessionId);
            return Forbid();
        }
    }

    /// <summary>
    /// Get attendance summary statistics.
    /// Results are filtered based on user role and permissions.
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Attendance summary statistics</returns>
    /// <response code="200">Returns the attendance summary</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetAttendanceSummary([FromQuery] AttendanceFilterRequest filter)
    {
        logger.LogInformation("Getting attendance summary");

        var summary = await attendanceService.GetAttendanceSummaryAsync(filter, User);
        logger.LogInformation("Successfully retrieved attendance summary");
        return Ok(summary);
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Update an existing attendance record.
    /// Only admins and instructors can update attendance records.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <param name="request">The update request</param>
    /// <returns>The updated attendance record</returns>
    /// <response code="200">Returns the updated attendance record</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="404">Attendance record not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> UpdateAttendance(int id, [FromBody] UpdateAttendanceRequest request)
    {
        logger.LogInformation("Updating attendance record with ID: {Id}", id);

        try
        {
            var updated = await attendanceService.UpdateAttendanceAsync(id, request, User);
            logger.LogInformation("Successfully updated attendance record with ID: {Id}", id);
            return Ok(updated);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Attendance record with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to update attendance record {Id}", id);
            return Forbid();
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete an attendance record.
    /// Only admins can delete attendance records.
    /// </summary>
    /// <param name="id">The attendance record ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Attendance record deleted successfully</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Forbidden - only admins can delete</response>
    /// <response code="404">Attendance record not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAttendance(int id)
    {
        logger.LogInformation("Deleting attendance record with ID: {Id}", id);

        try
        {
            var deleted = await attendanceService.DeleteAttendanceAsync(id, User);
            if (deleted)
            {
                logger.LogInformation("Successfully deleted attendance record with ID: {Id}", id);
                return NoContent();
            }

            logger.LogWarning("Failed to delete attendance record with ID: {Id}", id);
            return NotFound(new { message = "Attendance record not found" });
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Attendance record with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to delete attendance record {Id}", id);
            return Forbid();
        }
    }

    #endregion
}
