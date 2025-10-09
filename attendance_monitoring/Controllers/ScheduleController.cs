using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
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
        public async Task<ActionResult<IEnumerable<Schedules>>> GetSchedules()
        {
            logger.LogInformation("Getting all schedules");
            try
            {
                var schedules = await scheduleService.GetAllSchedulesAsync();
                logger.LogInformation("Successfully retrieved {Count} schedules", schedules.Count());
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while retrieving all schedules");
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
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
        public async Task<ActionResult<Schedules>> GetSchedule(int id)
        {
            logger.LogInformation("Getting schedule with ID: {Id}", id);
            try
            {
                var schedule = await scheduleService.GetScheduleByIdAsync(id);
                logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return Ok(schedule);
            }
            catch (ScheduleNotFoundException ex)
            {
                logger.LogWarning("Schedule with ID {Id} not found", id);
                return NotFound(ex.Message);
            }
            catch (ScheduleServiceException ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedule with ID {Id}", id);
                return Problem(
                    detail: "An error occurred while retrieving the schedule",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while retrieving schedule with ID {Id}", id);
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
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

            try
            {
                var (schedule, error) = await scheduleService.CreateScheduleAsync(createSchedule);

                if (error != null)
                {
                    logger.LogWarning("Schedule creation failed: {Error}", error);
                    return BadRequest(error);
                }

                if (schedule == null)
                {
                    logger.LogError("Schedule creation failed: Unexpected error occurred");
                    return BadRequest("An unexpected error occurred while creating the schedule.");
                }

                logger.LogInformation("Successfully created schedule with ID: {Id} and TimeIn: {TimeIn}, TimeOut: {TimeOut}", schedule.Id, schedule.TimeIn, schedule.TimeOut);
                return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
            }
            catch (ScheduleServiceException ex)
            {
                logger.LogError(ex, "Error occurred while creating schedule with TimeIn: {TimeIn}, TimeOut: {TimeOut}", createSchedule.TimeIn, createSchedule.TimeOut);
                return Problem(
                    detail: "An error occurred while creating the schedule",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while creating schedule with TimeIn: {TimeIn}, TimeOut: {TimeOut}",
                    createSchedule.TimeIn, createSchedule.TimeOut);
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
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
                    return BadRequest(error);
                }

                logger.LogInformation("Successfully updated schedule with ID: {Id}", id);
                return Ok(schedule);
            }
            catch (ScheduleNotFoundException ex)
            {
                logger.LogWarning("Schedule update failed: Schedule with ID {Id} not found", id);
                return NotFound(ex.Message);
            }
            catch (ScheduleServiceException ex)
            {
                logger.LogError(ex, "Error occurred while updating schedule with ID {Id}", id);
                return Problem(
                    detail: "An error occurred while updating the schedule",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while updating schedule with ID {Id}", id);
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
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
                return BadRequest(error);
            }
            catch (ScheduleNotFoundException ex)
            {
                logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                return NotFound(ex.Message);
            }
            catch (ScheduleServiceException ex)
            {
                logger.LogError(ex, "Error occurred while deleting schedule with ID {Id}", id);
                return Problem(
                    detail: "An error occurred while deleting the schedule",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while deleting schedule with ID {Id}", id);
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }
        }

        #endregion
    }
}