using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

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

    // GET: api/Student
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents([FromQuery] PaginationQuery paginationQuery)
    {
        var students = await _studentService.GetAllStudentsAsync(paginationQuery);
        return Ok(students);
    }

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
    [HttpPost("")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<Student>> CreateStudent(CreateStudent createStudent)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (student, error) = await _studentService.CreateStudentAsync(createStudent, User);

        if (error != null)
        {
            return BadRequest(error);
        }

        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
    }
    
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
}