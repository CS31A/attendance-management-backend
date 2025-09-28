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
        try
        {
            var subjects = await subjectService.GetAllSubjectsAsync();
            logger.LogInformation("Successfully retrieved {Count} subjects", subjects.Count());
            return Ok(subjects);
        }
        catch (SubjectServiceException ex)
        {
            logger.LogError(ex, "Error occurred while retrieving all subjects");
            return Problem(
                detail: "An error occurred while retrieving subjects",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while retrieving all subjects");
            return Problem(
                detail: "An unexpected error occurred",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
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
    public async Task<ActionResult<Subject>> GetSubject(int id)
    {
        logger.LogInformation("Getting subject with ID: {Id}", id);
        try
        {
            var subject = await subjectService.GetSubjectByIdAsync(id);
            logger.LogInformation("Successfully retrieved subject with ID: {Id}", id);
            return Ok(subject);
        }
        catch (SubjectNotFoundException ex)
        {
            logger.LogWarning("Subject with ID {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (SubjectServiceException ex)
        {
            logger.LogError(ex, "Error occurred while retrieving subject with ID {Id}", id);
            return Problem(
                detail: "An error occurred while retrieving the subject",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while retrieving subject with ID {Id}", id);
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

        try
        {
            var (subject, error) = await subjectService.CreateSubjectAsync(createSubject);

            if (error != null)
            {
                logger.LogWarning("Subject creation failed: {Error}", error);
                return BadRequest(error);
            }

            if (subject == null)
            {
                logger.LogError("Subject creation failed: Unexpected error occurred");
                return BadRequest("An unexpected error occurred while creating the subject.");
            }

            logger.LogInformation("Successfully created subject with ID: {Id} and name: {SubjectName}", subject.Id,
                subject.Name);
            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
        }
        catch (SubjectServiceException ex)
        {
            logger.LogError(ex, "Error occurred while creating subject with name: {SubjectName}", createSubject.Name);
            return Problem(
                detail: "An error occurred while creating the subject",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while creating subject with name: {SubjectName}",
                createSubject.Name);
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
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Subject>> UpdateSubject(int id, UpdateSubject updateSubject)
    {
        logger.LogInformation("Updating subject with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Subject update failed due to invalid model state for subject ID: {Id}", id);
            return BadRequest(ModelState);
        }

        try
        {
            var (subject, error) = await subjectService.UpdateSubjectAsync(id, updateSubject);

            if (error != null)
            {
                logger.LogWarning("Subject update failed for subject ID {Id}: {Error}", id, error);
                return BadRequest(error);
            }

            logger.LogInformation("Successfully updated subject with ID: {Id}", id);
            return Ok(subject);
        }
        catch (SubjectNotFoundException ex)
        {
            logger.LogWarning("Subject update failed: Subject with ID {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (SubjectServiceException ex)
        {
            logger.LogError(ex, "Error occurred while updating subject with ID {Id}", id);
            return Problem(
                detail: "An error occurred while updating the subject",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while updating subject with ID {Id}", id);
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
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteSubject(int id)
    {
        logger.LogInformation("Deleting subject with ID: {Id}", id);
        try
        {
            var error = await subjectService.DeleteSubjectAsync(id);

            if (error == null)
            {
                logger.LogInformation("Successfully deleted subject with ID: {Id}", id);
                return NoContent();
            }

            logger.LogWarning("Subject deletion failed for subject ID {Id}: {Error}", id, error);
            return BadRequest(error);
        }
        catch (SubjectNotFoundException ex)
        {
            logger.LogWarning("Subject deletion failed: Subject with ID {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (SubjectServiceException ex)
        {
            logger.LogError(ex, "Error occurred while deleting subject with ID {Id}", id);
            return Problem(
                detail: "An error occurred while deleting the subject",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while deleting subject with ID {Id}", id);
            return Problem(
                detail: "An unexpected error occurred",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    #endregion
}