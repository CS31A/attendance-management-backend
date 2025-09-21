using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing student records
/// </summary>
[Authorize(Policy = "UserPolicy")]
[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    private ActionResult<SoftDeleteResponse> CreateResponse(string? error, string successMessage)
    {
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

    /// <summary>
    /// Get a list of all students
    /// </summary>
    /// <returns>A list of students</returns>
    /// <response code="200">Returns the list of students</response>
    // GET: api/Student
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        _logger.LogInformation("Getting all students");
        var students = await _studentService.GetAllStudentsAsync();
        _logger.LogInformation("Successfully retrieved {Count} students", students.ToList().Count);
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
        _logger.LogInformation("Getting student with ID: {Id}", id);
        var student = await _studentService.GetStudentByIdAsync(id);

        if (student == null)
        {
            _logger.LogWarning("Student with ID {Id} not found", id);
            return NotFound();
        }

        _logger.LogInformation("Successfully retrieved student with ID: {Id}", id);
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
        _logger.LogInformation("Updating student with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Student update failed due to invalid model state for student ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var (student, error) = await _studentService.UpdateStudentAsync(id, updateStudent, User);

        if (error != null)
        {
            if (error.Contains("not found"))
            {
                _logger.LogWarning("Student update failed: Student with ID {Id} not found", id);
                return NotFound(error);
            }
            if (error.Contains("not authorized"))
            {
                _logger.LogWarning("Student update failed: User not authorized to update student with ID {Id}", id);
                return Unauthorized(error);
            }
            _logger.LogWarning("Student update failed for student ID {Id}: {Error}", id, error);
            return BadRequest(error);
        }

        _logger.LogInformation("Successfully updated student with ID: {Id}", id);
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
        _logger.LogInformation("Soft deleting student with ID: {Id}", id);
        var error = await _studentService.SoftDeleteStudentAsync(id, User);
        _logger.LogInformation("Soft delete operation completed for student with ID: {Id}", id);
        return CreateResponse(error, "Student marked as deleted successfully");
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
        _logger.LogInformation("Hard deleting student with ID: {Id}", id);
        var error = await _studentService.HardDeleteStudentAsync(id, User);
        _logger.LogInformation("Hard delete operation completed for student with ID: {Id}", id);
        return CreateResponse(error, "Student permanently deleted successfully");
    }
}