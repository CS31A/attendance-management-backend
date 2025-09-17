using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InstructorController(IInstructorService instructorService) : ControllerBase
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

    // GET: api/Instructor
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetInstructors([FromQuery] PaginationQuery paginationQuery)
    {
        var instructors = await instructorService.GetAllInstructorsAsync(paginationQuery);
        return Ok(instructors);
    }

    // GET: api/Instructor/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructor(int id)
    {
        var instructor = await instructorService.GetInstructorByIdAsync(id);

        if (instructor == null)
        {
            return NotFound();
        }

        return Ok(instructor);
    }
    
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
    [HttpPatch("{id}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<Instructor>> PatchInstructor(int id, UpdateInstructor updateInstructor)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (instructor, error) = await instructorService.UpdateInstructorAsync(id, updateInstructor, User);

        if (error == null) return Ok(instructor);
        if (error.Contains("not found"))
        {
            return NotFound(error);
        }
        if (error.Contains("not authorized"))
        {
            return Unauthorized(error);
        }
        return BadRequest(error);

    }

    // PATCH: api/Instructor/{id}/soft-delete
    [HttpPatch("{id}/soft-delete")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteInstructor(int id)
    {
        var error = await instructorService.SoftDeleteInstructorAsync(id, User);
        return CreateResponse(error ?? string.Empty, "Instructor marked as deleted successfully");
    }

    // DELETE: api/Instructor/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> HardDeleteInstructor(int id)
    {
        var error = await instructorService.HardDeleteInstructorAsync(id, User);
        return CreateResponse(error ?? string.Empty, "Instructor permanently deleted successfully");
    }
}