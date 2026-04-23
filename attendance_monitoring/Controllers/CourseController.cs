using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="500">Internal server error</response>
    // GET: api/Course
    [HttpGet]
    public async Task<ActionResult<IList<Course>>> GetCourses()
    {
        try
        {
            logger.LogInformation("Getting all courses");
            var courses = await courseService.GetAllCoursesAsync();
            logger.LogInformation("Successfully retrieved {Count} courses", courses.Count);
            return Ok(courses);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving courses");
            return StatusCode(500, "An error occurred while retrieving courses");
        }
    }

    /// <summary>
    /// Get a specific course by ID
    /// </summary>
    /// <param name="id">The ID of the course to retrieve</param>
    /// <returns>The requested course</returns>
    /// <response code="200">Returns the requested course</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    // GET: api/Course/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Course>> GetCourse(int id)
    {
        try
        {
            logger.LogInformation("Getting course with ID: {Id}", id);
            var course = await courseService.GetCourseByIdAsync(id);
            logger.LogInformation("Successfully retrieved course with ID: {Id}", id);
            return Ok(course);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Course with ID {Id} not found", id);
            return NotFound($"Course with ID {id} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving course with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the course");
        }
    }

    [HttpGet("uuid/{uuid:guid}")]
    public async Task<ActionResult<Course>> GetCourseByUuid(Guid uuid)
    {
        try
        {
            logger.LogInformation("Getting course with UUID: {Uuid}", uuid);
            var course = await courseService.GetCourseByUuidAsync(uuid);
            logger.LogInformation("Successfully retrieved course with UUID: {Uuid}", uuid);
            return Ok(course);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Course with UUID {Uuid} not found", uuid);
            return NotFound($"Course with UUID {uuid} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while retrieving course with UUID: {Uuid}", uuid);
            return StatusCode(500, "An error occurred while retrieving the course");
        }
    }

    /// <summary>
    /// Create a new course
    /// </summary>
    /// <param name="createCourse">The course data to create</param>
    /// <returns>The created course</returns>
    /// <response code="201">Returns the created course</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Not authorized to create courses</response>
    /// <response code="500">Internal server error</response>
    // POST: api/Course
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Course>> CreateCourse(CreateCourse createCourse)
    {
        try
        {
            logger.LogInformation("Creating new course with name: {CourseName}", createCourse.Name);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Course creation failed due to invalid model state");
                return BadRequest(ModelState);
            }

            var course = await courseService.CreateCourseAsync(createCourse, User);
            logger.LogInformation("Successfully created course with ID: {Id} and name: {CourseName}", course.Id, course.Name);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while creating course");
            return BadRequest(ex.Message);
        }
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
    /// <response code="500">Internal server error</response>
    // PUT: api/Course/5
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Course>> UpdateCourse(int id, UpdateCourse updateCourse)
    {
        try
        {
            logger.LogInformation("Updating course with ID: {Id}", id);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Course update failed due to invalid model state for course ID: {Id}", id);
                return BadRequest(ModelState);
            }

            var course = await courseService.UpdateCourseAsync(id, updateCourse, User);
            logger.LogInformation("Successfully updated course with ID: {Id}", id);
            return Ok(course);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Course with ID {Id} not found", id);
            return NotFound($"Course with ID {id} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while updating course with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("uuid/{uuid:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<Course>> UpdateCourseByUuid(Guid uuid, UpdateCourse updateCourse)
    {
        try
        {
            logger.LogInformation("Updating course with UUID: {Uuid}", uuid);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Course update failed due to invalid model state for course UUID: {Uuid}", uuid);
                return BadRequest(ModelState);
            }

            var course = await courseService.UpdateCourseByUuidAsync(uuid, updateCourse, User);
            logger.LogInformation("Successfully updated course with UUID: {Uuid}", uuid);
            return Ok(course);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Course with UUID {Uuid} not found", uuid);
            return NotFound($"Course with UUID {uuid} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while updating course with UUID: {Uuid}", uuid);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a course by ID
    /// </summary>
    /// <param name="id">The ID of the course to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Course deleted successfully</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Not authorized to delete courses</response>
    /// <response code="500">Internal server error</response>
    // DELETE: api/Course/5
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteCourse(int id)
    {
        try
        {
            logger.LogInformation("Deleting course with ID: {Id}", id);
            await courseService.DeleteCourseAsync(id, User);
            logger.LogInformation("Successfully deleted course with ID: {Id}", id);
            return NoContent();
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Course with ID {Id} not found", id);
            return NotFound($"Course with ID {id} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while deleting course with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("uuid/{uuid:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult> DeleteCourseByUuid(Guid uuid)
    {
        try
        {
            logger.LogInformation("Deleting course with UUID: {Uuid}", uuid);
            await courseService.DeleteCourseByUuidAsync(uuid, User);
            logger.LogInformation("Successfully deleted course with UUID: {Uuid}", uuid);
            return NoContent();
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            logger.LogWarning(ex, "Course with UUID {Uuid} not found", uuid);
            return NotFound($"Course with UUID {uuid} not found");
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while deleting course with UUID: {Uuid}", uuid);
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "PrivilegedPolicy")]
    [HttpGet("{id:int}/has-sections")]
    public async Task<ActionResult<bool>> HasSectionsInCourse(int id)
    {
        try
        {
            if (id <= 0)
            {
                logger.LogWarning("Invalid course ID {CourseId} provided for dependency check.", id);
                return BadRequest("Course ID must be greater than 0.");
            }

            var hasSections = await courseService.HasSectionsInCourseAsync(id);
            return Ok(hasSections);
        }
        catch (EntityServiceException ex)
        {
            logger.LogError(ex, "Service error occurred while checking sections for course with ID {CourseId}", id);
            return StatusCode(500, "An error occurred while checking course dependencies");
        }
    }
}
