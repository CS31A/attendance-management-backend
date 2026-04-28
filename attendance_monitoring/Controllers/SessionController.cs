using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing session operations.
/// Sessions represent actual class occurrences and track attendance.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sessions")]
public class SessionController(ISessionService sessionService, ILogger<SessionController> logger) : ControllerBase
{
    #region Get Operations

    /// <summary>
    /// Get all sessions.
    /// </summary>
    /// <returns>A list of sessions</returns>
    /// <response code="200">Returns the list of sessions</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetAllSessions()
    {
        logger.LogInformation("Getting all sessions");

        var sessions = await sessionService.GetAllSessionsAsync();
        logger.LogInformation("Successfully retrieved {Count} sessions", sessions.Count());
        return Ok(sessions);
    }

    /// <summary>
    /// Get all sessions belonging to the current instructor.
    /// </summary>
    /// <returns>A list of sessions for the current instructor</returns>
    /// <response code="200">Returns the list of sessions for the instructor</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Instructor profile not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-sessions")]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetMySessions()
    {
        logger.LogInformation("Getting sessions for the current instructor");

        var sessions = await sessionService.GetMySessionsAsync();
        logger.LogInformation("Successfully retrieved {Count} sessions for the current instructor", sessions.Count());
        return Ok(sessions);
    }

    /// <summary>
    /// Get a specific session by ID.
    /// </summary>
    /// <param name="id">The ID of the session to retrieve</param>
    /// <returns>The requested session</returns>
    /// <response code="200">Returns the requested session</response>
    /// <response code="404">Session not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<SessionResponseDto>> GetSession(Guid id)
    {
        logger.LogInformation("Getting session with ID: {Id}", id);

        try
        {
            var session = await sessionService.GetSessionByIdAsync(id);
            logger.LogInformation("Successfully retrieved session with ID: {Id}", id);
            return Ok(session);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Session with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionResponseDto>> GetSessionByUuid([FromRoute(Name = "id")] Guid id)
    {
        logger.LogInformation("Getting session with UUID: {Id}", id);

        try
        {
            var session = await sessionService.GetSessionByUuidAsync(id);
            logger.LogInformation("Successfully retrieved session with UUID: {Id}", id);
            return Ok(session);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Session with UUID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized session UUID access attempt for {Id}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message, errorCode = "FORBIDDEN" });
        }
    }

    /// <summary>
    /// Get sessions for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID</param>
    /// <returns>A list of sessions for the schedule</returns>
    /// <response code="200">Returns the list of sessions</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("schedule/{scheduleId:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetSessionsBySchedule(Guid scheduleId)
    {
        logger.LogInformation("Getting sessions for schedule ID: {ScheduleId}", scheduleId);

        var sessions = await sessionService.GetSessionsByScheduleIdAsync(scheduleId);
        logger.LogInformation("Successfully retrieved {Count} sessions for schedule ID: {ScheduleId}",
            sessions.Count(), scheduleId);
        return Ok(sessions);
    }

    [HttpGet("schedule/{id:guid}")]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetSessionsByScheduleUuid([FromRoute(Name = "id")] Guid scheduleUuid)
    {
        logger.LogInformation("Getting sessions for schedule UUID: {ScheduleUuid}", scheduleUuid);

        var sessions = await sessionService.GetSessionsByScheduleUuidAsync(scheduleUuid);
        logger.LogInformation("Successfully retrieved {Count} sessions for schedule UUID: {ScheduleUuid}",
            sessions.Count(), scheduleUuid);
        return Ok(sessions);
    }

    /// <summary>
    /// Get sessions by status.
    /// </summary>
    /// <returns>A list of sessions with the specified status</returns>
    /// <response code="200">Returns the list of sessions</response>
    /// <response code="400">Invalid status value</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetSessionsByStatus(string status)
    {
        logger.LogInformation("Getting sessions with status: {Status}", status);

        if (!SessionStatusConstants.IsValid(status))
        {
            logger.LogWarning("Invalid status value: {Status}", status);
            return BadRequest(new
            {
                message = $"Invalid status value. Valid values are: {string.Join(", ", SessionStatusConstants.All)}"
            });
        }

        var normalizedStatus = SessionStatusConstants.Normalize(status);
        var sessions = await sessionService.GetSessionsByStatusAsync(normalizedStatus);
        logger.LogInformation("Successfully retrieved {Count} sessions with status: {Status}",
            sessions.Count(), normalizedStatus);
        return Ok(sessions);
    }

    /// <summary>
    /// Get sessions for a specific date.
    /// </summary>
    /// <param name="date">The session date (YYYY-MM-DD format)</param>
    /// <returns>A list of sessions on the specified date</returns>
    /// <response code="200">Returns the list of sessions</response>
    /// <response code="400">Invalid date format</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("date/{date}")]
    public async Task<ActionResult<IEnumerable<SessionResponseDto>>> GetSessionsByDate(DateTime date)
    {
        logger.LogInformation("Getting sessions for date: {Date:yyyy-MM-dd}", date);

        var sessions = await sessionService.GetSessionsByDateAsync(date);
        logger.LogInformation("Successfully retrieved {Count} sessions for date: {Date:yyyy-MM-dd}",
            sessions.Count(), date);
        return Ok(sessions);
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Update the actual room for a session.
    /// Only active sessions can have their room updated.
    /// This addresses Issue #6 from the implementation requirements.
    /// </summary>
    /// <param name="id">The ID of the session to update</param>
    /// <param name="updateRequest">The room update request</param>
    /// <returns>The updated session</returns>
    /// <response code="200">Returns the updated session</response>
    /// <response code="400">Invalid input data or session not in active status</response>
    /// <response code="404">Session or classroom not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Not authorized to update this session</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id:int}/room")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> UpdateSessionRoom(Guid id, UpdateSessionRoom updateRequest)
    {
        logger.LogInformation("Updating room for session ID: {SessionId} to classroom ID: {ClassroomId}",
            id, updateRequest.ActualRoomId);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session room update failed due to invalid model state for session ID: {SessionId}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.UpdateSessionRoomAsync(id, updateRequest);
        logger.LogInformation("Successfully updated room for session ID: {SessionId}", id);
        return Ok(session);
        // Exceptions are handled by global exception handler
    }

    [HttpPatch("{id:guid}/room")]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> UpdateSessionRoomByUuid([FromRoute(Name = "id")] Guid id, UpdateSessionRoom updateRequest)
    {
        logger.LogInformation("Updating room for session UUID: {SessionUuid}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session room update failed due to invalid model state for session UUID: {SessionUuid}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.UpdateSessionRoomByUuidAsync(id, updateRequest);
        logger.LogInformation("Successfully updated room for session UUID: {SessionUuid}", id);
        return Ok(session);
    }

    #endregion

    #region Lifecycle Management Operations

    /// <summary>
    /// Create a new session for a schedule.
    /// </summary>
    /// <param name="request">The create session request</param>
    /// <returns>The created session</returns>
    /// <response code="201">Returns the newly created session</response>
    /// <response code="400">Invalid input data or business rule violation</response>
    /// <response code="404">Schedule not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> CreateSession([FromBody] CreateSession request)
    {
        var sessionDate = request.SessionDate ?? DateTime.Today;
        logger.LogInformation("Creating session for schedule ID: {ScheduleId} on date: {SessionDate:yyyy-MM-dd}",
            request.ScheduleId, sessionDate);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session creation failed due to invalid model state");
            return BadRequest(ModelState);
        }

        var session = await sessionService.CreateSessionAsync(request);
        logger.LogInformation("Successfully created session ID: {SessionId}", session.Id);
        return CreatedAtAction(nameof(GetSessionByUuid), new { id = session.Id }, session);
        // Exceptions are handled by global exception handler
    }

    /// <summary>
    /// Start a session, marking it as active.
    /// </summary>
    /// <param name="id">The ID of the session to start</param>
    /// <param name="request">The start session request</param>
    /// <returns>The updated session</returns>
    /// <response code="200">Returns the started session</response>
    /// <response code="400">Invalid input data or session cannot be started</response>
    /// <response code="404">Session not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Not authorized to start this session</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id:int}/start")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> StartSession(Guid id, [FromBody] StartSession request)
    {
        logger.LogInformation("Starting session ID: {SessionId}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session start failed due to invalid model state for session ID: {SessionId}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.StartSessionAsync(id, request);
        logger.LogInformation("Successfully started session ID: {SessionId}", id);
        return Ok(session);
        // Exceptions are handled by global exception handler
    }

    [HttpPatch("{id:guid}/start")]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> StartSessionByUuid([FromRoute(Name = "id")] Guid id, [FromBody] StartSession request)
    {
        logger.LogInformation("Starting session UUID: {SessionUuid}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session start failed due to invalid model state for session UUID: {SessionUuid}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.StartSessionByUuidAsync(id, request);
        logger.LogInformation("Successfully started session UUID: {SessionUuid}", id);
        return Ok(session);
    }

    /// <summary>
    /// End an active session.
    /// </summary>
    /// <param name="id">The ID of the session to end</param>
    /// <param name="request">The end session request</param>
    /// <returns>The updated session</returns>
    /// <response code="200">Returns the ended session</response>
    /// <response code="400">Invalid input data or session cannot be ended</response>
    /// <response code="404">Session not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Not authorized to end this session</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id:int}/end")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> EndSession(Guid id, [FromBody] EndSession request)
    {
        logger.LogInformation("Ending session ID: {SessionId}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session end failed due to invalid model state for session ID: {SessionId}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.EndSessionAsync(id, request);
        logger.LogInformation("Successfully ended session ID: {SessionId}", id);
        return Ok(session);
        // Exceptions are handled by global exception handler
    }

    [HttpPatch("{id:guid}/end")]
    [Authorize(Policy = "InstructorPolicy")]
    public async Task<ActionResult<SessionResponseDto>> EndSessionByUuid([FromRoute(Name = "id")] Guid id, [FromBody] EndSession request)
    {
        logger.LogInformation("Ending session UUID: {SessionUuid}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session end failed due to invalid model state for session UUID: {SessionUuid}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.EndSessionByUuidAsync(id, request);
        logger.LogInformation("Successfully ended session UUID: {SessionUuid}", id);
        return Ok(session);
    }

    /// <summary>
    /// Cancel a session that has not started yet.
    /// </summary>
    /// <param name="id">The ID of the session to cancel</param>
    /// <param name="request">The cancel session request</param>
    /// <returns>The cancelled session</returns>
    /// <response code="200">Returns the cancelled session</response>
    /// <response code="400">Invalid input data or session cannot be cancelled</response>
    /// <response code="404">Session not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">Not authorized to cancel this session</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SessionResponseDto>> CancelSession(Guid id, [FromBody] CancelSession request)
    {
        logger.LogInformation("Cancelling session ID: {SessionId}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session cancellation failed due to invalid model state for session ID: {SessionId}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.CancelSessionAsync(id, request);
        logger.LogInformation("Successfully cancelled session ID: {SessionId}", id);
        return Ok(session);
        // Exceptions are handled by global exception handler
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SessionResponseDto>> CancelSessionByUuid([FromRoute(Name = "id")] Guid id, [FromBody] CancelSession request)
    {
        logger.LogInformation("Cancelling session UUID: {SessionUuid}", id);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Session cancellation failed due to invalid model state for session UUID: {SessionUuid}", id);
            return BadRequest(ModelState);
        }

        var session = await sessionService.CancelSessionByUuidAsync(id, request);
        logger.LogInformation("Successfully cancelled session UUID: {SessionUuid}", id);
        return Ok(session);
    }

    #endregion
}
