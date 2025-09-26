using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing course records
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CourseController(ICourseService courseService, ILogger<CourseController> logger) : ControllerBase
{
    /// <summary>
    /// Get a list of all courses
    /// </summary>
    /// <returns>A list of courses</returns>
    /// <response code="200">Returns the list of courses</response>
    // GET: api/Course
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
    {
        logger.LogInformation("Getting all courses");
        var courses = await courseService.GetAllCoursesAsync();
        logger.LogInformation("Successfully retrieved {Count} courses", courses.ToList().Count);
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
        logger.LogInformation("Getting course with ID: {Id}", id);
        var course = await courseService.GetCourseByIdAsync(id);

        if (course == null)
        {
            logger.LogWarning("Course with ID {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("Successfully retrieved course with ID: {Id}", id);
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
        logger.LogInformation("Creating new course with name: {CourseName}", createCourse.Name);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Course creation failed due to invalid model state");
            return BadRequest(ModelState);
        }

        var (course, error) = await courseService.CreateCourseAsync(createCourse, User);

        if (error != null)
        {
            logger.LogWarning("Course creation failed: {Error}", error);
            return BadRequest(error);
        }
        
        if (course == null)
        {
            logger.LogError("Course creation failed: Unexpected error occurred");
            return BadRequest("An unexpected error occurred while creating the course.");
        }

        logger.LogInformation("Successfully created course with ID: {Id} and name: {CourseName}", course.Id, course.Name);
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
        logger.LogInformation("Updating course with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Course update failed due to invalid model state for course ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var (course, error) = await courseService.UpdateCourseAsync(id, updateCourse, User);

        if (error != null)
        {
            if (error.Contains("not found"))
            {
                logger.LogWarning("Course update failed: Course with ID {Id} not found", id);
                return NotFound(error);
            }
            logger.LogWarning("Course update failed for course ID {Id}: {Error}", id, error);
            return BadRequest(error);
        }

        logger.LogInformation("Successfully updated course with ID: {Id}", id);
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
        logger.LogInformation("Deleting course with ID: {Id}", id);
        var error = await courseService.DeleteCourseAsync(id, User);

        if (error == null) 
        {
            logger.LogInformation("Successfully deleted course with ID: {Id}", id);
            return NoContent();
        }
        if (error.Contains("not found"))
        {
            logger.LogWarning("Course deletion failed: Course with ID {Id} not found", id);
            return NotFound(error);
        }
        logger.LogWarning("Course deletion failed for course ID {Id}: {Error}", id, error);
        return BadRequest(error);

    }
}