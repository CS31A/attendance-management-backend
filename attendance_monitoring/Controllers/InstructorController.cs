using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly IInstructorService _instructorService;

    public InstructorController(IInstructorService instructorService)
    {
        _instructorService = instructorService;
    }

    // GET: api/Instructor
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instructor>>> GetInstructors([FromQuery] PaginationQuery paginationQuery)
    {
        var instructors = await _instructorService.GetAllInstructorsAsync(paginationQuery);
        return Ok(instructors);
    }

    // GET: api/Instructor/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Instructor>> GetInstructor(int id)
    {
        var instructor = await _instructorService.GetInstructorByIdAsync(id);

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

        var (instructor, error) = await _instructorService.UpdateInstructorAsync(id, updateInstructor, User);

        if (error != null)
        {
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

        return Ok(instructor);
    }
}