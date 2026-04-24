using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller exposing aggregated report endpoints for attendance analytics.
/// All endpoints respect role-based data scoping enforced in the service layer.
/// </summary>
[Authorize(Policy = "UserPolicy")]
[ApiController]
[Route("api/reports")]
public class ReportsController(IReportsService reportsService, ILogger<ReportsController> logger) : ControllerBase
{
    /// <summary>
    /// Returns aggregate attendance summary statistics.
    /// Accepts optional filters: startDate, endDate, sectionId, studentId, sessionId.
    /// Accessible to Admin, Instructors, and Students (students can only view their own data).
    /// </summary>
    [HttpGet("attendance-summary")]
    [ProducesResponseType(typeof(AttendanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceSummaryDto>> GetAttendanceSummary([FromQuery] AttendanceFilterRequest filter)
    {
        logger.LogInformation("Getting attendance summary report");
        try
        {
            var result = await reportsService.GetAttendanceSummaryAsync(filter, User);
            return Ok(result);
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning(ex, "Entity not found while getting attendance summary");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to attendance summary");
            return Forbid();
        }
    }

    /// <summary>
    /// Returns attendance history and statistics for a specific student.
    /// Accessible to Admin, Instructors, and Students (students can only view their own data).
    /// </summary>
    [HttpGet("student-attendance/{id:guid}")]
    [ProducesResponseType(typeof(StudentAttendanceHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAttendanceHistoryDto>> GetStudentAttendanceReport([FromRoute(Name = "id")] Guid studentId)
    {
        logger.LogInformation("Getting student attendance report for StudentId: {StudentId}", studentId);
        try
        {
            var result = await reportsService.GetStudentAttendanceReportAsync(studentId, User);
            return Ok(result);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Student {StudentId} not found", studentId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to student {StudentId} report", studentId);
            return Forbid();
        }
    }

    /// <summary>
    /// Returns full attendance overview for a specific session.
    /// Instructors can only view their own sessions.
    /// </summary>
    [HttpGet("session-attendance/{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(SessionAttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionAttendanceDto>> GetSessionAttendanceReport([FromRoute(Name = "id")] Guid sessionId)
    {
        logger.LogInformation("Getting session attendance report for SessionId: {SessionId}", sessionId);
        try
        {
            var result = await reportsService.GetSessionAttendanceReportAsync(sessionId, User);
            return Ok(result);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Session {SessionId} not found", sessionId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to session {SessionId} report", sessionId);
            return Forbid();
        }
    }

    /// <summary>
    /// Returns section-level attendance summary with per-session breakdown.
    /// Accepts optional filters: startDate, endDate.
    /// </summary>
    [HttpGet("class-attendance/{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(ClassAttendanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClassAttendanceSummaryDto>> GetClassAttendanceReport(
        [FromRoute(Name = "id")] Guid sectionId, [FromQuery] AttendanceFilterRequest filter)
    {
        logger.LogInformation("Getting class attendance report for SectionId: {SectionId}", sectionId);
        try
        {
            var result = await reportsService.GetClassAttendanceReportAsync(sectionId, filter, User);
            return Ok(result);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Section {SectionId} not found", sectionId);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning(ex, "Entity not found while getting class attendance report");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to section {SectionId} attendance report", sectionId);
            return Forbid();
        }
    }

    /// <summary>
    /// Returns session list with per-session attendance counts for a specific instructor.
    /// Accepts optional filters: startDate, endDate.
    /// </summary>
    [HttpGet("instructor-sessions/{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(InstructorSessionsReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InstructorSessionsReportDto>> GetInstructorSessionsReport(
        [FromRoute(Name = "id")] Guid instructorId, [FromQuery] AttendanceFilterRequest filter)
    {
        logger.LogInformation("Getting instructor sessions report for InstructorId: {InstructorId}", instructorId);
        try
        {
            var result = await reportsService.GetInstructorSessionsReportAsync(instructorId, filter, User);
            return Ok(result);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Instructor {InstructorId} not found", instructorId);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning(ex, "Entity not found while getting instructor sessions report");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to instructor {InstructorId} sessions report", instructorId);
            return Forbid();
        }
    }
}
