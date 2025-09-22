using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[Authorize]
[ApiController]
[Route("api/instructors")]
public class InstructorController(IInstructorService instructorService, ILogger<InstructorController> logger) : ControllerBase
{
    private ActionResult<SoftDeleteResponse> CreateResponse(string error, string successMessage)
    {
        // Handle success case
        if (string.IsNullOrEmpty(error))
        {
            return Ok(new SoftDeleteResponse
            {
                Success = true,
                Message = successMessage
            });
        }

        if (error.Contains("not found"))
        {
            return NotFound(new SoftDeleteResponse
            {
                Success = false,
                Message = error
            });
        }

        if (error.Contains("not authorized"))
        {
            return Unauthorized(new SoftDeleteResponse
            {
                Success = false,
                Message = error
            });
        }

        return BadRequest(new SoftDeleteResponse
        {
            Success = false,
            Message = error
        });
    }

    #region Read Operations

    // GET: api/Instructor
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetInstructors()
    {
        logger.LogInformation("Getting all instructors");
        var instructors = await instructorService.GetAllInstructorsAsync();
        logger.LogInformation("Successfully retrieved {Count} instructors", instructors.ToList().Count);
        return Ok(instructors);
    }

    // GET: api/Instructor/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Instructor>> GetInstructor(int id)
    {
        logger.LogInformation("Getting instructor with ID: {Id}", id);
        var instructor = await instructorService.GetInstructorByIdAsync(id);

        if (instructor == null)
        {
            logger.LogWarning("Instructor with ID {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("Successfully retrieved instructor with ID: {Id}", id);
        return Ok(instructor);
    }

    #endregion

    #region Update Operations
    
    // POST: api/Instructor/
    // [HttpPost("")]
    // public async Task<ActionResult<Instructor>> CreateInstructor(CreateInstructor createInstructor)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return BadRequest(ModelState);
    //     }

    //     var (instructor, error) = await _instructorService.CreateInstructorAsync(createInstructor, User);

    //     if (error != null)
    //     {
    //         return BadRequest(error);
    //     }

    //     return CreatedAtAction(nameof(GetInstructor), new { id = instructor.Id }, instructor);
    // }
    
    // REDUNDANT ENDPOINT: This endpoint is redundant because instructor records are automatically 
    // created during user registration when a user registers with the "Teacher" role. Additionally, 
    // this endpoint had a security issue as it allowed any authenticated user to create instructor 
    // records. Do not remove this code block entirely as it might be needed for future 
    // administrative purposes, but it's currently commented out to prevent confusion and 
    // potential security vulnerabilities.
    
    // PATCH: api/Instructor/{id}
    [HttpPatch("{id:int}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<Instructor>> PatchInstructor(int id, UpdateInstructor updateInstructor)
    {
        logger.LogInformation("Updating instructor with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Instructor update failed due to invalid model state for instructor ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var (instructor, error) = await instructorService.UpdateInstructorAsync(id, updateInstructor, User);

        if (error == null) 
        {
            logger.LogInformation("Successfully updated instructor with ID: {Id}", id);
            return Ok(instructor);
        }
        if (error.Contains("not found"))
        {
            logger.LogWarning("Instructor update failed: Instructor with ID {Id} not found", id);
            return NotFound(error);
        }
        if (error.Contains("not authorized"))
        {
            logger.LogWarning("Instructor update failed: User not authorized to update instructor with ID {Id}", id);
            return Unauthorized(error);
        }
        logger.LogWarning("Instructor update failed for instructor ID {Id}: {Error}", id, error);
        return BadRequest(error);

    }

    #endregion

    #region Delete Operations

    // PATCH: api/Instructor/{id}/soft-delete
    [HttpPatch("{id:int}/soft-delete")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteInstructor(int id)
    {
        logger.LogInformation("Soft deleting instructor with ID: {Id}", id);
        var error = await instructorService.SoftDeleteInstructorAsync(id, User);
        logger.LogInformation("Soft delete operation completed for instructor with ID: {Id}", id);
        return CreateResponse(error ?? string.Empty, "Instructor marked as deleted successfully");
    }

    // DELETE: api/Instructor/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> HardDeleteInstructor(int id)
    {
        logger.LogInformation("Hard deleting instructor with ID: {Id}", id);
        var error = await instructorService.HardDeleteInstructorAsync(id, User);
        logger.LogInformation("Hard delete operation completed for instructor with ID: {Id}", id);
        return CreateResponse(error ?? string.Empty, "Instructor permanently deleted successfully");
    }
    
    // PATCH: api/Instructor/{id}/restore
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> RestoreInstructor(int id)
    {
        logger.LogInformation("Restoring instructor with ID: {Id}", id);
        var error = await instructorService.RestoreInstructorAsync(id, User);
        logger.LogInformation("Restore operation completed for instructor with ID: {Id}", id);
        return CreateResponse(error ?? string.Empty, "Instructor restored successfully");
    }

    #endregion
}