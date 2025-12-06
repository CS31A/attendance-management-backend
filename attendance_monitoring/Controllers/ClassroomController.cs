using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing classroom records
/// </summary>
[Authorize(Policy = "PrivilegedPolicy")]
[ApiController]
[Route("api/classrooms")]
public class ClassroomController(IClassroomService classroomService, ILogger<ClassroomController> logger) : ControllerBase
{
    #region Get Operations

    /// <summary>
    /// Get a list of all classrooms
    /// </summary>
    /// <returns>A list of classrooms</returns>
    /// <response code="200">Returns the list of classrooms</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Classrooms
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Classroom>>> GetClassrooms()
    {
        logger.LogInformation("Getting all classrooms");

        var classrooms = await classroomService.GetAllClassroomsAsync();
        logger.LogInformation("Successfully retrieved {Count} classrooms", classrooms.Count());
        return Ok(classrooms);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Get a specific classroom by ID
    /// </summary>
    /// <param name="id">The ID of the classroom to retrieve</param>
    /// <returns>The requested classroom</returns>
    /// <response code="200">Returns the requested classroom</response>
    /// <response code="404">Classroom not found</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Classrooms/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Classroom>> GetClassroom(int id)
    {
        logger.LogInformation("Getting classroom with ID: {Id}", id);
        try
        {
            var classroom = await classroomService.GetClassroomByIdAsync(id);
            logger.LogInformation("Successfully retrieved classroom with ID: {Id}", id);
            return Ok(classroom);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Classroom with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        // No generic catch - global handler will manage unexpected errors
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Create a new classroom
    /// </summary>
    /// <param name="createClassroom">The classroom data to create</param>
    /// <returns>The created classroom</returns>
    /// <response code="201">Returns the created classroom</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Not authorized to create classrooms</response>
    /// <response code="500">Internal server error</response>
    // POST: api/Classrooms
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Classroom>> CreateClassroom(CreateClassroom createClassroom)
    {
        logger.LogInformation("Creating new classroom with name: {ClassroomName}", createClassroom.Name);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Classroom creation failed due to invalid model state");
            return BadRequest(ModelState);
        }

        var classroom = await classroomService.CreateClassroomAsync(createClassroom);

        logger.LogInformation("Successfully created classroom with ID: {Id} and name: {ClassroomName}", classroom.Id,
            classroom.Name);
        return CreatedAtAction(nameof(GetClassroom), new { id = classroom.Id }, classroom);
        // Exceptions are handled by global exception handler
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Update a classroom record
    /// </summary>
    /// <param name="id">The ID of the classroom to update</param>
    /// <param name="updateClassroom">The updated classroom data</param>
    /// <returns>The updated classroom</returns>
    /// <response code="200">Returns the updated classroom</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="404">Classroom not found</response>
    /// <response code="401">Not authorized to update this classroom</response>
    /// <response code="500">Internal server error</response>
    // PATCH: api/Classrooms/5
    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Classroom>> UpdateClassroom(int id, UpdateClassroom updateClassroom)
    {
        logger.LogInformation("Updating classroom with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Classroom update failed due to invalid model state for classroom ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var classroom = await classroomService.UpdateClassroomAsync(id, updateClassroom);

        logger.LogInformation("Successfully updated classroom with ID: {Id}", id);
        return Ok(classroom);
        // Exceptions are handled by global exception handler
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete a classroom by ID
    /// </summary>
    /// <param name="id">The ID of the classroom to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Classroom deleted successfully</response>
    /// <response code="404">Classroom not found</response>
    /// <response code="401">Not authorized to delete classrooms</response>
    /// <response code="500">Internal server error</response>
    // DELETE: api/Classrooms/5
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteClassroom(int id)
    {
        logger.LogInformation("Deleting classroom with ID: {Id}", id);

        await classroomService.DeleteClassroomAsync(id);

        logger.LogInformation("Successfully deleted classroom with ID: {Id}", id);
        return NoContent();
        // Exceptions are handled by global exception handler
    }

    #endregion
}
