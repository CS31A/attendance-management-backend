using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing student records
/// </summary>
[Authorize(Policy = "UserPolicy")]
[ApiController]
[Route("api/students")]
public class StudentController(IStudentService studentService, ILogger<StudentController> logger)
    : ControllerBase
{
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
    /// Get a list of all non-deleted students
    /// </summary>
    /// <returns>A list of non-deleted students</returns>
    /// <response code="200">Returns the list of non-deleted students</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Student
    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpGet]
    public async Task<ActionResult<IList<Student>>> GetStudents()
    {
        try
        {
            logger.LogInformation("Getting all non-deleted students");
            var students = await studentService.GetAllNonDeletedStudentsAsync();
            logger.LogInformation("Successfully retrieved {Count} non-deleted students", students.Count);
            return Ok(students);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving students");
            return StatusCode(500, "An error occurred while retrieving students");
        }
    }

    /// <summary>
    /// Get a specific student by ID
    /// </summary>
    /// <param name="id">The ID of the student to retrieve</param>
    /// <returns>The requested student</returns>
    /// <response code="200">Returns the requested student</response>
    /// <response code="404">Student not found</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Student/5
    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        try
        {
            logger.LogInformation("Getting student with ID: {Id}", id);
            var student = await studentService.GetStudentByIdAsync(id);
            logger.LogInformation("Successfully retrieved student with ID: {Id}", id);
            return Ok(student);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Student with ID {Id} not found", id);
            return NotFound($"Student with ID {id} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving student with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the student");
        }
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
    /// <response code="500">Internal server error</response>
    // PATCH: api/Student/{id}
    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<Student>> PatchStudent(int id, UpdateStudent updateStudent)
    {
        try
        {
            logger.LogInformation("Updating student with ID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Student update failed due to invalid model state for student ID: {Id}", id);
                return BadRequest(ModelState);
            }

            var student = await studentService.UpdateStudentAsync(id, updateStudent, User);
            logger.LogInformation("Successfully updated student with ID: {Id}", id);
            return Ok(student);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Student with ID {Id} not found", id);
            return NotFound($"Student with ID {id} not found");
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "User not authorized to update student with ID {Id}", id);
            return Unauthorized(ex.Message);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while updating student with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Soft delete a student record
    /// </summary>
    /// <param name="id">The ID of the student to softly delete</param>
    /// <returns>Success message</returns>
    /// <response code="200">Student marked as deleted successfully</response>
    /// <response code="404">Student not found</response>
    /// <response code="401">Not authorized to delete this student</response>
    /// <response code="500">Internal server error</response>
    // PATCH: api/Student/{id}/soft-delete
    [Authorize(Policy = "AdminPolicy")]
    [HttpPatch("{id:int}/soft-delete")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteStudent(int id)
    {
        try
        {
            logger.LogInformation("Soft deleting student with ID: {Id}", id);
            await studentService.SoftDeleteStudentAsync(id, User);
            logger.LogInformation("Soft delete operation completed for student with ID: {Id}", id);
            return Ok(new SoftDeleteResponse
            {
                Success = true,
                Message = "Student marked as deleted successfully"
            });
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Student with ID {Id} not found", id);
            return NotFound(new SoftDeleteResponse
            {
                Success = false,
                Message = $"Student with ID {id} not found"
            });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "User not authorized to delete student with ID {Id}", id);
            return Unauthorized(new SoftDeleteResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while soft deleting student with ID: {Id}", id);
            return BadRequest(new SoftDeleteResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
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
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> HardDeleteStudent(int id)
    {
        logger.LogInformation("Hard deleting student with ID: {Id}", id);
        var error = await studentService.HardDeleteStudentAsync(id, User);
        logger.LogInformation("Hard delete operation completed for student with ID: {Id}", id);
        return CreateResponse(error, "Student permanently deleted successfully");
    }

    /// <summary>
    /// Restore a soft deleted student record
    /// </summary>
    /// <param name="id">The ID of the student to restore</param>
    /// <returns>Success message</returns>
    /// <response code="200">Student restored successfully</response>
    /// <response code="404">Student not found</response>
    /// <response code="401">Not authorized to restore this student</response>
    // PATCH: api/Students/{id}/restore
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> RestoreStudent(int id)
    {
        logger.LogInformation("Restoring student with ID: {Id}", id);
        var error = await studentService.RestoreStudentAsync(id, User);
        logger.LogInformation("Restore operation completed for student with ID: {Id}", id);
        return CreateResponse(error, "Student restored successfully");
    }

    /// <summary>
    /// Get all subjects assigned to the authenticated student
    /// </summary>
    /// <returns>A list of subjects with schedule details for the student</returns>
    /// <response code="200">Returns the list of assigned subjects</response>
    /// <response code="401">User is not authenticated or not a student</response>
    /// <response code="404">Student record not found</response>
    /// <response code="500">Internal server error</response>
    // GET: api/students/my-subjects
    [HttpGet("my-subjects")]
    [Authorize] // Any authenticated user can access this, but it will only work for students
    public async Task<ActionResult<IEnumerable<StudentSubjectResponseDto>>> GetMySubjects()
    {
        try
        {
            logger.LogInformation("Getting subjects for authenticated student");
            var subjects = await studentService.GetStudentSubjectsAsync(User);
            logger.LogInformation("Successfully retrieved subjects for student");
            return Ok(subjects);
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning(ex, "Student record not found for authenticated user");
            return NotFound("Student record not found. This endpoint is only available for students.");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving student subjects");
            return StatusCode(500, "An error occurred while retrieving your subjects");
        }
    }

    /// <summary>
    /// Search students by name with pagination
    /// </summary>
    /// <param name="query">The search term to match against first or last name (minimum 2 characters)</param>
    /// <param name="pageNumber">The page number (1-based, default: 1)</param>
    /// <param name="pageSize">The number of items per page (1-100, default: 50)</param>
    /// <returns>A paginated list of students matching the search criteria</returns>
    /// <response code="200">Returns the paginated list of students (empty list if no matches)</response>
    /// <response code="400">Invalid query parameter or pagination values</response>
    /// <response code="401">User is not authenticated or not authorized</response>
    /// <response code="500">Internal server error</response>
    // GET: api/students/search/name?query=john&pageNumber=1&pageSize=50
    [HttpGet("search/name")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<IEnumerable<Student>>> SearchStudentsByName(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                logger.LogWarning("Student name search failed: Query parameter is required");
                return BadRequest("Query parameter is required");
            }

            logger.LogInformation("Searching students by name with query: {Query}, page: {PageNumber}, size: {PageSize}",
                query, pageNumber, pageSize);

            var students = await studentService.SearchStudentsByNameAsync(query, pageNumber, pageSize);

            logger.LogInformation("Successfully retrieved students for name search query: {Query}", query);
            return Ok(students);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while searching students by name");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Search students by email with pagination
    /// </summary>
    /// <param name="query">The search term to match against user email (minimum 2 characters)</param>
    /// <param name="pageNumber">The page number (1-based, default: 1)</param>
    /// <param name="pageSize">The number of items per page (1-100, default: 50)</param>
    /// <returns>A paginated list of students matching the search criteria</returns>
    /// <response code="200">Returns the paginated list of students (empty list if no matches)</response>
    /// <response code="400">Invalid query parameter or pagination values</response>
    /// <response code="401">User is not authenticated or not authorized</response>
    /// <response code="500">Internal server error</response>
    // GET: api/students/search/email?query=@example.com&pageNumber=1&pageSize=50
    [HttpGet("search/email")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<IEnumerable<Student>>> SearchStudentsByEmail(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                logger.LogWarning("Student email search failed: Query parameter is required");
                return BadRequest("Query parameter is required");
            }

            logger.LogInformation("Searching students by email with query: {Query}, page: {PageNumber}, size: {PageSize}",
                query, pageNumber, pageSize);

            var students = await studentService.SearchStudentsByEmailAsync(query, pageNumber, pageSize);

            logger.LogInformation("Successfully retrieved students for email search query: {Query}", query);
            return Ok(students);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while searching students by email");
            return BadRequest(ex.Message);
        }
    }
}
