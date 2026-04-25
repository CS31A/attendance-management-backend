using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing subject records
/// </summary>
[Authorize(Policy = "PrivilegedPolicy")]
[ApiController]
[Route("api/subjects")]
public class SubjectController(ISubjectService subjectService, ILogger<SubjectController> logger) : ControllerBase
{
    #region Get Operations

    /// <summary>
    /// Get a list of all subjects
    /// </summary>
    /// <returns>A list of subjects</returns>
    /// <response code="200">Returns the list of subjects</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Subject
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects()
    {
        logger.LogInformation("Getting all subjects");

        var subjects = await subjectService.GetAllSubjectsAsync();
        logger.LogInformation("Successfully retrieved {Count} subjects", subjects.Count());
        return Ok(subjects);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Get a specific subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to retrieve</param>
    /// <returns>The requested subject</returns>
    /// <response code="200">Returns the requested subject</response>
    /// <response code="404"> not found</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Subject/5
    [HttpGet("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<Subject>> GetSubject(int id)
    {
        logger.LogInformation("Getting subject with ID: {Id}", id);
        try
        {
            var subject = await subjectService.GetSubjectByIdAsync(id);
            logger.LogInformation("Successfully retrieved subject with ID: {Id}", id);
            return Ok(subject);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Subject with ID {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        // No generic catch - global handler will manage unexpected errors
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Subject>> GetSubjectByUuid([FromRoute(Name = "id")] Guid uuid)
    {
        logger.LogInformation("Getting subject with UUID: {Uuid}", uuid);
        try
        {
            var subject = await subjectService.GetSubjectByUuidAsync(uuid);
            logger.LogInformation("Successfully retrieved subject with UUID: {Uuid}", uuid);
            return Ok(subject);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Subject with UUID {Uuid} not found", uuid);
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Create a new subject
    /// </summary>
    /// <param name="createSubject">The subject data to create</param>
    /// <returns>The created subject</returns>
    /// <response code="201">Returns the created subject</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Not authorized to create subjects</response>
    /// <response code="500">Internal server error</response>
    // POST: api/Subject
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Subject>> CreateSubject(CreateSubject createSubject)
    {
        logger.LogInformation("Creating new subject with name: {SubjectName}", createSubject.Name);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Subject creation failed due to invalid model state");
            return BadRequest(ModelState);
        }

        var subject = await subjectService.CreateSubjectAsync(createSubject);

        logger.LogInformation("Successfully created subject with ID: {Id} and name: {SubjectName}", subject.Id,
            subject.Name);
        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
        // Exceptions are handled by global exception handler
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Update a subject record
    /// </summary>
    /// <param name="id">The ID of the subject to update</param>
    /// <param name="updateSubject">The updated subject data</param>
    /// <returns>The updated subject</returns>
    /// <response code="200">Returns the updated subject</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="404">Subject not found</response>
    /// <response code="401">Not authorized to update this subject</response>
    /// <response code="500">Internal server error</response>
    // PATCH: api/Subject/5
    [HttpPatch("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Subject>> UpdateSubject(int id, UpdateSubject updateSubject)
    {
        logger.LogInformation("Updating subject with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Subject update failed due to invalid model state for subject ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var subject = await subjectService.UpdateSubjectAsync(id, updateSubject);

        logger.LogInformation("Successfully updated subject with ID: {Id}", id);
        return Ok(subject);
        // Exceptions are handled by global exception handler
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Subject>> UpdateSubjectByUuid([FromRoute(Name = "id")] Guid uuid, UpdateSubject updateSubject)
    {
        logger.LogInformation("Updating subject with UUID: {Uuid}", uuid);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Subject update failed due to invalid model state for subject UUID: {Uuid}", uuid);
            return BadRequest(ModelState);
        }

        var subject = await subjectService.UpdateSubjectByUuidAsync(uuid, updateSubject);

        logger.LogInformation("Successfully updated subject with UUID: {Uuid}", uuid);
        return Ok(subject);
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete a subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Subject deleted successfully</response>
    /// <response code="404">Subject not found</response>
    /// <response code="401">Not authorized to delete subjects</response>
    /// <response code="500">Internal server error</response>
    // DELETE: api/Subject/5
    [HttpDelete("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteSubject(int id)
    {
        logger.LogInformation("Deleting subject with ID: {Id}", id);

        await subjectService.DeleteSubjectAsync(id);

        logger.LogInformation("Successfully deleted subject with ID: {Id}", id);
        return NoContent();
        // Exceptions are handled by global exception handler
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteSubjectByUuid([FromRoute(Name = "id")] Guid uuid)
    {
        logger.LogInformation("Deleting subject with UUID: {Uuid}", uuid);

        await subjectService.DeleteSubjectByUuidAsync(uuid);

        logger.LogInformation("Successfully deleted subject with UUID: {Uuid}", uuid);
        return NoContent();
    }

    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpGet("{id:int}/has-schedules")]
    public async Task<ActionResult<bool>> HasSchedulesInSubject(int id)
    {
        try
        {
            if (id <= 0)
            {
                logger.LogWarning("Invalid subject ID {SubjectId} provided for dependency check.", id);
                return BadRequest("Subject ID must be greater than 0.");
            }

            var hasSchedules = await subjectService.HasSchedulesInSubjectAsync(id);
            return Ok(hasSchedules);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while checking schedules for subject with ID {SubjectId}", id);
            return StatusCode(500, "An error occurred while checking subject dependencies");
        }
    }

    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpGet("{id:int}/has-enrollments")]
    public async Task<ActionResult<bool>> HasEnrollmentsInSubject(int id)
    {
        try
        {
            if (id <= 0)
            {
                logger.LogWarning("Invalid subject ID {SubjectId} provided for dependency check.", id);
                return BadRequest("Subject ID must be greater than 0.");
            }

            var hasEnrollments = await subjectService.HasEnrollmentsInSubjectAsync(id);
            return Ok(hasEnrollments);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while checking enrollments for subject with ID {SubjectId}", id);
            return StatusCode(500, "An error occurred while checking subject dependencies");
        }
    }

    #endregion
}
