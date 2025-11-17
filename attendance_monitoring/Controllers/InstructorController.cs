using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
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
        try
        {
            logger.LogInformation("Getting all instructors");
            var instructors = await instructorService.GetAllInstructorsAsync();
            logger.LogInformation("Successfully retrieved {Count} instructors", instructors.ToList().Count);
            return Ok(instructors);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving instructors");
            return StatusCode(500, "An error occurred while retrieving instructors");
        }
    }

    // GET: api/Instructor/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Instructor>> GetInstructor(int id)
    {
        try
        {
            logger.LogInformation("Getting instructor with ID: {Id}", id);
            var instructor = await instructorService.GetInstructorByIdAsync(id);
            logger.LogInformation("Successfully retrieved instructor with ID: {Id}", id);
            return Ok(instructor);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Instructor with ID {Id} not found", id);
            return NotFound($"Instructor with ID {id} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving instructor with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the instructor");
        }
    }

    // GET: api/instructors/{instructorId}/subjects
    [HttpGet("{instructorId:int}/subjects")]
    public async Task<ActionResult<IEnumerable<SubjectResponseDto>>> GetInstructorSubjects(int instructorId)
    {
        try
        {
            logger.LogInformation("Getting subjects for instructor ID: {InstructorId}", instructorId);
            var subjects = await instructorService.GetSubjectsByInstructorIdAsync(instructorId);
            logger.LogInformation("Successfully retrieved subjects for instructor ID: {InstructorId}", instructorId);
            return Ok(subjects);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Instructor with ID {InstructorId} not found", instructorId);
            return NotFound($"Instructor with ID {instructorId} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving subjects for instructor ID: {InstructorId}", instructorId);
            return StatusCode(500, "An error occurred while retrieving the subjects");
        }
    }

    // GET: api/instructors/profile
    [HttpGet("profile")]
    public async Task<ActionResult<InstructorProfileResponseDto>> GetInstructorProfile()
    {
        try
        {
            logger.LogInformation("Getting instructor profile for authenticated user");
            var profile = await instructorService.GetInstructorProfileAsync(User);
            
            if (profile == null)
            {
                logger.LogWarning("No instructor profile found for authenticated user");
                return NotFound("No instructor profile found for the current user");
            }

            logger.LogInformation("Successfully retrieved instructor profile with ID: {InstructorId}", profile.Id);
            return Ok(profile);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving instructor profile");
            return StatusCode(500, "An error occurred while retrieving the instructor profile");
        }
    }

    // GET: api/instructors/me/schedules
    [HttpGet("me/schedules")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetMySchedules()
    {
        try
        {
            logger.LogInformation("Getting schedules for authenticated instructor");
            var schedules = await instructorService.GetSchedulesByInstructorAsync(User);
            logger.LogInformation("Successfully retrieved {Count} schedules for authenticated instructor", schedules.Count());
            return Ok(schedules);
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning(ex, "Instructor not found for authenticated user");
            return NotFound("No instructor record found for the current user");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving instructor schedules");
            return StatusCode(500, "An error occurred while retrieving the schedules");
        }
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
        try
        {
            logger.LogInformation("Updating instructor with ID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Instructor update failed due to invalid model state for instructor ID: {Id}", id);
                return BadRequest(ModelState);
            }

            var instructor = await instructorService.UpdateInstructorAsync(id, updateInstructor, User);
            logger.LogInformation("Successfully updated instructor with ID: {Id}", id);
            return Ok(instructor);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Instructor with ID {Id} not found", id);
            return NotFound($"Instructor with ID {id} not found");
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "User not authorized to update instructor with ID {Id}", id);
            return Unauthorized(ex.Message);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while updating instructor with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
    }

    #endregion

    #region Delete Operations

    // PATCH: api/Instructor/{id}/soft-delete
    [HttpPatch("{id:int}/soft-delete")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteInstructor(int id)
    {
        try
        {
            logger.LogInformation("Soft deleting instructor with ID: {Id}", id);
            await instructorService.SoftDeleteInstructorAsync(id, User);
            logger.LogInformation("Soft delete operation completed for instructor with ID: {Id}", id);
            return Ok(new SoftDeleteResponse
            {
                Success = true,
                Message = "Instructor marked as deleted successfully"
            });
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Instructor with ID {Id} not found", id);
            return NotFound(new SoftDeleteResponse
            {
                Success = false,
                Message = $"Instructor with ID {id} not found"
            });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "User not authorized to delete instructor with ID {Id}", id);
            return Unauthorized(new SoftDeleteResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while soft deleting instructor with ID: {Id}", id);
            return BadRequest(new SoftDeleteResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
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