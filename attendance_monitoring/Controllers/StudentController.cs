using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing student records
/// </summary>
[Authorize(Policy = "UserPolicy")]
[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Get a list of students with pagination
    /// </summary>
    /// <param name="paginationQuery">Pagination parameters</param>
    /// <returns>A list of students</returns>
    /// <response code="200">Returns the list of students</response>
    // GET: api/Student
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents([FromQuery] PaginationQuery paginationQuery)
    {
        var students = await _studentService.GetAllStudentsAsync(paginationQuery);
        return Ok(students);
    }

    /// <summary>
    /// Get a specific student by ID
    /// </summary>
    /// <param name="id">The ID of the student to retrieve</param>
    /// <returns>The requested student</returns>
    /// <response code="200">Returns the requested student</response>
    /// <response code="404"> not found</response>
    // GET: api/Student/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        return Ok(student);
    }
    
    // POST: api/Students/
    // [HttpPost("")]
    // [Authorize(Policy = "PrivilegedPolicy")]
    // public async Task<ActionResult<Student>> CreateStudent(CreateStudent createStudent)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return BadRequest(ModelState);
    //     }

    //     var (student, error) = await _studentService.CreateStudentAsync(createStudent, User);

    //     if (error != null)
    //     {
    //         return BadRequest(error);
    //     }

    //     return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
    // }
    
    // REDUNDANT ENDPOINT: This endpoint is redundant because student records are automatically 
    // created during user registration. All new users default to "Student" role and get a 
    // student record created automatically. Do not remove this code block entirely as it 
    // might be needed for future administrative purposes, but it's currently commented out 
    // to prevent confusion and potential misuse.
    
    /// <summary>
    /// Update a student record
    /// </summary>
    /// <param name="id">The ID of the student to update</param>
    /// <param name="updateStudent">The updated student data</param>
    /// <returns>The updated student</returns>
    /// <response code="200">Returns the updated student</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="404">Student not found</response>
    /// <response code="401">Not authorized to update this student</response>
    // PATCH: api/Student/{id}
    [HttpPatch("{id}")]
    public async Task<ActionResult<Student>> PatchStudent(int id, UpdateStudent updateStudent)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (student, error) = await _studentService.UpdateStudentAsync(id, updateStudent, User);

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

        return Ok(student);
    }

    /// <summary>
    /// Soft delete a student record
    /// </summary>
    /// <param name="id">The ID of the student to soft delete</param>
    /// <returns>Success message</returns>
    /// <response code="200">Student marked as deleted successfully</response>
    /// <response code="404">Student not found</response>
    /// <response code="401">Not authorized to delete this student</response>
    // PATCH: api/Student/{id}/soft-delete
    [HttpPatch("{id}/soft-delete")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteStudent(int id)
    {
        var error = await _studentService.SoftDeleteStudentAsync(id, User);

        if (error != null)
        {
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

        return Ok(new SoftDeleteResponse 
        { 
            Success = true, 
            Message = "Student marked as deleted successfully" 
        });
    }

    /// <summary>
    /// Hard delete a student record
    /// </summary>
    /// <param name="id">The ID of the student to hard delete</param>
    /// <returns>Success message</returns>
    /// <response code="200">Student permanently deleted successfully</response>
    /// <response code="404">Student not found</response>
    /// <response code="401">Not authorized to permanently delete this student</response>
    // DELETE: api/Student/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> HardDeleteStudent(int id)
    {
        var error = await _studentService.HardDeleteStudentAsync(id, User);

        if (error != null)
        {
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

        return Ok(new SoftDeleteResponse 
        { 
            Success = true, 
            Message = "Student permanently deleted successfully" 
        });
    }
}