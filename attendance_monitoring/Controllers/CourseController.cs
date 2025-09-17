using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing course records
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CourseController(ICourseService courseService) : ControllerBase
{
    /// <summary>
    /// Get a list of courses with pagination
    /// </summary>
    /// <param name="paginationQuery">Pagination parameters</param>
    /// <returns>A list of courses</returns>
    /// <response code="200">Returns the list of courses</response>
    // GET: api/Course
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetCourses([FromQuery] PaginationQuery paginationQuery)
    {
        var courses = await courseService.GetAllCoursesAsync(paginationQuery);
        return Ok(courses);
    }

    /// <summary>
    /// Get a specific course by ID
    /// </summary>
    /// <param name="id">The ID of the course to retrieve</param>
    /// <returns>The requested course</returns>
    /// <response code="200">Returns the requested course</response>
    /// <response code="404">Course not found</response>
    // GET: api/Course/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourse(int id)
    {
        var course = await courseService.GetCourseByIdAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    /// <summary>
    /// Create a new course
    /// </summary>
    /// <param name="createCourse">The course data to create</param>
    /// <returns>The created course</returns>
    /// <response code="201">Returns the created course</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Not authorized to create courses</response>
    // POST: api/Course
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Course>> CreateCourse(CreateCourse createCourse)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (course, error) = await courseService.CreateCourseAsync(createCourse, User);

        if (error != null)
        {
            return BadRequest(error);
        }
        
        if (course == null)
        {
            return BadRequest("An unexpected error occurred while creating the course.");
        }

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    /// <summary>
    /// Update a course record
    /// </summary>
    /// <param name="id">The ID of the course to update</param>
    /// <param name="updateCourse">The updated course data</param>
    /// <returns>The updated course</returns>
    /// <response code="200">Returns the updated course</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Not authorized to update this course</response>
    // PUT: api/Course/5
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Course>> UpdateCourse(int id, UpdateCourse updateCourse)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (course, error) = await courseService.UpdateCourseAsync(id, updateCourse, User);

        if (error != null)
        {
            if (error.Contains("not found"))
            {
                return NotFound(error);
            }
            return BadRequest(error);
        }

        return Ok(course);
    }

    /// <summary>
    /// Delete a course by ID
    /// </summary>
    /// <param name="id">The ID of the course to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Course deleted successfully</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Not authorized to delete courses</response>
    // DELETE: api/Course/5
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteCourse(int id)
    {
        var error = await courseService.DeleteCourseAsync(id, User);

        if (error == null) return NoContent();
        if (error.Contains("not found"))
        {
            return NotFound(error);
        }
        return BadRequest(error);

    }
}