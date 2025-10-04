using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing course-related operations
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly UserContextService _userContextService;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// Initializes a new instance of the CourseService class
    /// </summary>
    /// <param name="courseRepository">Repository for course data operations</param>
    /// <param name="userContextService">Service for managing user context and authorization</param>
    /// <param name="logger">Logger for logging operations</param>
    public CourseService(ICourseRepository courseRepository, UserContextService userContextService, ILogger<CourseService> logger)
    {
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all courses
    /// </summary>
    /// <returns>A collection of courses</returns>
    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        _logger.LogInformation("Retrieving all courses");
        var courses = await _courseRepository.GetAllCoursesAsync().ConfigureAwait(false);
        var allCoursesAsync = courses.ToList();
        _logger.LogInformation("Successfully retrieved {Count} courses", allCoursesAsync.ToList().Count);
        return allCoursesAsync;
    }

    /// <summary>
    /// Retrieves a specific course by ID
    /// </summary>
    /// <param name="id">The ID of the course to retrieve</param>
    /// <returns>The course with the specified ID, or null if not found</returns>
    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving course by ID: {Id}", id);
        var course = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
        if (course == null)
        {
            _logger.LogWarning("Course with ID {Id} not found", id);
        }
        else
        {
            _logger.LogInformation("Successfully retrieved course with ID: {Id}", id);
        }
        return course;
    }

    /// <summary>
    /// Creates a new course record
    /// </summary>
    /// <param name="createCourse">The course data to create</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>A tuple containing the created course (if successful) and an error message (if any)</returns>
    public async Task<(Course?, string?)> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user)
    {
        _logger.LogInformation("Creating new course with name: {CourseName}", createCourse.Name);

        if (string.IsNullOrWhiteSpace(createCourse.Name))
        {
            _logger.LogWarning("Course creation failed: Course name is required");
            return (null, "Course name is required");
        }

        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Course creation failed: User ID not found in token");
            return (null, "User ID not found in token");
        }

        var course = new Course
        {
            Name = createCourse.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var createdCourse = await _courseRepository.CreateCourse(course).ConfigureAwait(false);
            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created course with ID: {Id} and name: {CourseName}", createdCourse.Id, createdCourse.Name);
            return (createdCourse, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating course with name: {CourseName}", createCourse.Name);
            return (null, "An error occurred while creating the course. Please try again later.");
        }
    }

    /// <summary>
    /// Updates an existing course record
    /// </summary>
    /// <param name="id">The ID of the course to update</param>
    /// <param name="updateCourse">The updated course data</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>A tuple containing the updated course (if successful) and an error message (if any)</returns>
    public async Task<(Course?, string?)> UpdateCourseAsync(int id, UpdateCourse updateCourse, ClaimsPrincipal user)
    {
        _logger.LogInformation("Updating course with ID: {Id}", id);
        
        // Additional validation for defense in depth
        if (updateCourse == null)
        {
            _logger.LogWarning("Course update failed: Update course data is required");
            return (null, "Update course data is required");
        }

        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Course update failed: User ID not found in token");
            return (null, "User ID not found in token");
        }

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
        if (existingCourse == null)
        {
            _logger.LogWarning("Course update failed: Course with ID {Id} not found", id);
            return (null, "Course not found");
        }

        if (!string.IsNullOrEmpty(updateCourse.Name))
        {
            existingCourse.Name = updateCourse.Name;
        }

        existingCourse.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updatedCourse = await _courseRepository.UpdateCourseAsync(existingCourse).ConfigureAwait(false);
            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated course with ID: {Id}", id);
            return (updatedCourse, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating course with ID: {Id}", id);
            return (null, "An error occurred while updating the course. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes a course by ID
    /// </summary>
    /// <param name="id">The ID of the course to delete</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>An error message if deletion fails, null otherwise</returns>
    public async Task<string?> DeleteCourseAsync(int id, ClaimsPrincipal user)
    {
        _logger.LogInformation("Deleting course with ID: {Id}", id);
        
        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Course deletion failed: User ID not found in token");
            return "User ID not found in token";
        }

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
        if (existingCourse == null)
        {
            _logger.LogWarning("Course deletion failed: Course with ID {Id} not found", id);
            return "Course not found";
        }

        var result = await _courseRepository.DeleteCourseAsync(id).ConfigureAwait(false);
        if (!result)
        {
            _logger.LogError("Course deletion failed: Failed to delete course with ID {Id}", id);
            return "Failed to delete course";
        }

        try
        {
            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted course with ID: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting course with ID: {Id}", id);
            return "An error occurred while deleting the course. Please try again later.";
        }
    }
}