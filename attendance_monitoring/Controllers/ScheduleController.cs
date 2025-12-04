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
        /// Get a specific schedule by ID
        /// </summary>
        /// <param name="id">The ID of the schedule to retrieve</param>
        /// <returns>The requested schedule</returns>
        /// <response code="200">Returns the requested schedule</response>
        /// <response code="404"> not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ScheduleResponseDto>> GetSchedule(int id)
        {
            logger.LogInformation("Getting schedule with ID: {Id}", id);
            try
            {
                var schedule = await scheduleService.GetScheduleByIdAsync(id);
                logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return Ok(schedule);
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Schedule with ID {Id} not found", id);
                return NotFound(new { message = ex.Message });
            }
            // No generic catch - global handler will manage unexpected errors
        }

        /// <summary>
        /// Get all schedules assigned to a specific instructor
        /// </summary>
        /// <param name="instructorId">The ID of the instructor</param>
        /// <returns>A list of schedules assigned to the instructor</returns>
        /// <response code="200">Returns the list of schedules for the instructor</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{instructorId:int}/all")]
        public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetSchedulesByInstructor(int instructorId)
        {
            logger.LogInformation("Getting schedules for instructor ID: {InstructorId}", instructorId);

            var schedules = await scheduleService.GetSchedulesByInstructorIdAsync(instructorId);
            logger.LogInformation("Successfully retrieved {Count} schedules for instructor ID: {InstructorId}",
                schedules.Count(), instructorId);
            return Ok(schedules);
            // No try-catch - global handler will catch any unexpected errors
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

            try
            {
                var (schedule, error) = await scheduleService.CreateScheduleAsync(createSchedule);

                if (error != null)
                {
                    logger.LogWarning("Schedule creation failed: {Error}", error);
                    return BadRequest(new { message = error });
                }

                if (schedule == null)
                {
                    logger.LogWarning("Schedule creation failed: Unexpected error occurred");
                    return BadRequest(new { message = "An unexpected error occurred while creating the schedule." });
                }

                logger.LogInformation("Successfully created schedule with ID: {Id} and TimeIn: {TimeIn}, TimeOut: {TimeOut}", schedule.Id, schedule.TimeIn, schedule.TimeOut);
                return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Entity not found while creating schedule");
                return NotFound(new { message = ex.Message });
            }
            catch (EntityAlreadyExistsException<string> ex)
            {
                logger.LogWarning(ex, "Duplicate schedule detected");
                return Conflict(new { message = ex.Message });
            }
            catch (EntityAlreadyExistsException<int> ex)
            {
                logger.LogWarning(ex, "Duplicate schedule detected");
                return Conflict(new { message = ex.Message });
            }
            // No generic catch - global handler will manage unexpected errors
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
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Schedules>> UpdateSchedule(int id, UpdateSchedule updateSchedule)
        {
            logger.LogInformation("Updating schedule with ID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Schedule update failed due to invalid model state for schedule ID: {Id}", id);
                return BadRequest(ModelState);
            }

            try
            {
                var (schedule, error) = await scheduleService.UpdateScheduleAsync(id, updateSchedule);

                if (error != null)
                {
                    logger.LogWarning("Schedule update failed for schedule ID {Id}: {Error}", id, error);
                    return BadRequest(new { message = error });
                }

                logger.LogInformation("Successfully updated schedule with ID: {Id}", id);
                return Ok(schedule);
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Schedule update failed: Schedule with ID {Id} not found", id);
                return NotFound(new { message = ex.Message });
            }
            // No generic catch - global handler will manage unexpected errors
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
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult> DeleteSchedule(int id)
        {
            logger.LogInformation("Deleting schedule with ID: {Id}", id);
            try
            {
                var error = await scheduleService.DeleteScheduleAsync(id, User);

                if (error == null)
                {
                    logger.LogInformation("Successfully deleted schedule with ID: {Id}", id);
                    return NoContent();
                }

                logger.LogWarning("Schedule deletion failed for schedule ID {Id}: {Error}", id, error);
                return BadRequest(new { message = error });
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Schedule deletion failed: Schedule with ID {Id} not found", id);
                return NotFound(new { message = ex.Message });
            }
            catch (EntityUnauthorizedException ex)
            {
                logger.LogWarning(ex, "Unauthorized schedule deletion attempt for schedule ID {Id}", id);
                return Forbid();
            }
            // No generic catch - global handler will manage unexpected errors
        }

        #endregion
    }
}