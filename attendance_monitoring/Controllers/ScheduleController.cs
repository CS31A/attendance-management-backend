using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers
{
    /// <summary>
    /// Controller for managing schedule records
    /// </summary>
    [Authorize(Policy = "PrivilegedPolicy")]
    [ApiController]
    [Route("api/schedules")]
    public class ScheduleController(IScheduleService scheduleService, ILogger<ScheduleController> logger)
        : ControllerBase
    {
        #region Get Operations

        /// <summary>
        /// Get a list of all schedules
        /// </summary>
        /// <returns>A list of all schedules</returns>
        /// <response code="200">Returns the list of all schedules</response>
        /// <response code="500\">Internal server error</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetSchedules()
        {
            logger.LogInformation("Getting all schedules");

            var schedules = await scheduleService.GetAllSchedulesAsync();
            logger.LogInformation("Successfully retrieved {Count} schedules", schedules.Count());
            return Ok(schedules);
            // No try-catch - global handler will catch any unexpected errors
        }

        /// <summary>
        /// Get all schedules assigned to the current instructor
        /// </summary>
        /// <returns>A list of schedules assigned to the current instructor</returns>
        /// <response code="200">Returns the list of schedules for the instructor</response>
        /// <response code="401">Not authorized</response>
        /// <response code="403">Instructor profile not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("my-schedules")]
        [Authorize(Policy = "InstructorPolicy")]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetMySchedules()
        {
            logger.LogInformation("Getting schedules for the current instructor");

            var schedules = await scheduleService.GetMySchedulesAsync();
            logger.LogInformation("Successfully retrieved {Count} schedules for the current instructor",
                schedules.Count());
            return Ok(schedules);
            // No try-catch - global handler will catch any unexpected errors
        }

        /// <summary>
        /// Get a specific schedule by ID
        /// </summary>
        /// <param name="id">The ID of the schedule to retrieve</param>
        /// <returns>The requested schedule</returns>
        /// <response code="200">Returns the requested schedule</response>
        /// <response code="404"> not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ScheduleResponseDto>> GetSchedule(Guid id)
        {
            logger.LogInformation("Getting schedule with ID: {Id}", id);
            try
            {
                var schedule = await scheduleService.GetScheduleByIdAsync(id);
                logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return Ok(schedule);
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Schedule with ID {Id} not found", id);
                return NotFound(new { message = ex.Message });
            }
            // No generic catch - global handler will manage unexpected errors
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ScheduleResponseDto>> GetScheduleByUuid([FromRoute(Name = "id")] Guid id)
        {
            logger.LogInformation("Getting schedule with UUID: {Id}", id);
            try
            {
                var schedule = await scheduleService.GetScheduleByUuidAsync(id);
                logger.LogInformation("Successfully retrieved schedule with UUID: {Id}", id);
                return Ok(schedule);
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Schedule with UUID {Id} not found", id);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all schedules assigned to a specific instructor
        /// </summary>
        /// <param name="instructorId">The ID of the instructor</param>
        /// <returns>A list of schedules assigned to the instructor</returns>
        /// <response code="200">Returns the list of schedules for the instructor</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{instructorId:int}/all")]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetSchedulesByInstructor(Guid instructorId)
        {
            logger.LogInformation("Getting schedules for instructor ID: {InstructorId}", instructorId);

            var schedules = await scheduleService.GetSchedulesByInstructorIdAsync(instructorId);
            logger.LogInformation("Successfully retrieved {Count} schedules for instructor ID: {InstructorId}",
                schedules.Count(), instructorId);
            return Ok(schedules);
            // No try-catch - global handler will catch any unexpected errors
        }

        /// <summary>
        /// Get all schedules for a specific section
        /// </summary>
        /// <param name="sectionId">The ID of the section</param>
        /// <returns>A list of schedules for the section</returns>
        /// <response code="200">Returns the list of schedules for the section</response>
        /// <response code="400">Invalid request or error retrieving schedules</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("by-section/{sectionId:guid}")]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetSchedulesBySection(Guid sectionId)
        {
            logger.LogInformation("Getting schedules for section ID: {SectionId}", sectionId);

            var schedules = await scheduleService.GetSchedulesBySectionIdAsync(sectionId);
            logger.LogInformation("Successfully retrieved {Count} schedules for section ID: {SectionId}",
                schedules.Count(), sectionId);
            return Ok(schedules);
            // No try-catch - global handler will catch any unexpected errors
        }

        [Authorize(Policy = "PrivilegedPolicy")]
        [HttpGet("{id:guid}/has-sessions")]
        public async Task<ActionResult<bool>> HasSessionsInSchedule(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    logger.LogWarning("Invalid schedule ID {ScheduleId} provided for dependency check.", id);
                    return BadRequest("Schedule ID must be greater than 0.");
                }

                var hasSessions = await scheduleService.HasSessionsInScheduleAsync(id);
                return Ok(hasSessions);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while checking sessions for schedule with ID {ScheduleId}", id);
                return StatusCode(500, "An error occurred while checking schedule dependencies");
            }
        }

        #endregion

        #region Create Operations

        /// <summary>
        /// Create a new schedule
        /// </summary>
        /// <param name="createSchedule">The schedule data to create</param>
        /// <returns>The created schedule</returns>
        /// <response code="201">Returns the created schedule</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="401">Not authorized to create schedules</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Schedules>> PostSchedule(CreateSchedule createSchedule)
        {
            logger.LogInformation("Creating new schedule with TimeIn: {TimeIn}, TimeOut: {TimeOut}",
                createSchedule.TimeIn, createSchedule.TimeOut);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Schedule creation failed due to invalid model state");
                return BadRequest(ModelState);
            }

            var schedule = await scheduleService.CreateScheduleAsync(createSchedule);

            logger.LogInformation("Successfully created schedule with ID: {Id} and TimeIn: {TimeIn}, TimeOut: {TimeOut}", schedule.Id, schedule.TimeIn, schedule.TimeOut);
            return CreatedAtAction(nameof(GetScheduleByUuid), new { id = schedule.Id }, schedule);
            // Exceptions are handled by global exception handler
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Update an existing schedule
        /// </summary>
        /// <param name="id">The ID of the schedule to update</param>
        /// <param name="updateSchedule">The updated schedule data</param>
        /// <returns>The updated schedule</returns>
        /// <response code="200">Returns the updated schedule</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="404">Schedule not found</response>
        /// <response code="401">Not authorized to update this schedule</response>
        /// <response code="500">Internal server error</response>
        [HttpPatch("{id:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Schedules>> UpdateSchedule(Guid id, UpdateSchedule updateSchedule)
        {
            logger.LogInformation("Updating schedule with ID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Schedule update failed due to invalid model state for schedule ID: {Id}", id);
                return BadRequest(ModelState);
            }

            var schedule = await scheduleService.UpdateScheduleAsync(id, updateSchedule);

            logger.LogInformation("Successfully updated schedule with ID: {Id}", id);
            return Ok(schedule);
            // Exceptions are handled by global exception handler
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Schedules>> UpdateScheduleByUuid([FromRoute(Name = "id")] Guid id, UpdateSchedule updateSchedule)
        {
            logger.LogInformation("Updating schedule with UUID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Schedule update failed due to invalid model state for schedule UUID: {Id}", id);
                return BadRequest(ModelState);
            }

            var schedule = await scheduleService.UpdateScheduleByUuidAsync(id, updateSchedule);

            logger.LogInformation("Successfully updated schedule with UUID: {Id}", id);
            return Ok(schedule);
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Delete a schedule by ID
        /// </summary>
        /// <param name="id">The ID of the schedule to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Schedule deleted successfully</response>
        /// <response code="404">Schedule not found</response>
        /// <response code="401">Not authorized to delete schedules</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult> DeleteSchedule(Guid id)
        {
            logger.LogInformation("Deleting schedule with ID: {Id}", id);

            await scheduleService.DeleteScheduleAsync(id, User);

            logger.LogInformation("Successfully deleted schedule with ID: {Id}", id);
            return NoContent();
            // Exceptions are handled by global exception handler
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult> DeleteScheduleByUuid([FromRoute(Name = "id")] Guid id)
        {
            logger.LogInformation("Deleting schedule with UUID: {Id}", id);

            await scheduleService.DeleteScheduleByUuidAsync(id, User);

            logger.LogInformation("Successfully deleted schedule with UUID: {Id}", id);
            return NoContent();
        }

        #endregion
    }
}
