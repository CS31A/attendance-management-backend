using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing course-related operations
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly UserContextService _userContextService;

    /// <summary>
    /// Initializes a new instance of the CourseService class
    /// </summary>
    /// <param name="courseRepository">Repository for course data operations</param>
    /// <param name="userContextService">Service for managing user context and authorization</param>
    public CourseService(ICourseRepository courseRepository, UserContextService userContextService)
    {
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
    }

    /// <summary>
    /// Retrieves all courses with pagination support
    /// </summary>
    /// <param name="paginationQuery">Pagination parameters</param>
    /// <returns>A collection of courses</returns>
    public async Task<IEnumerable<Course>> GetAllCoursesAsync(PaginationQuery paginationQuery)
    {
        return await _courseRepository.GetAllCoursesAsync(paginationQuery);
    }

    /// <summary>
    /// Retrieves a specific course by ID
    /// </summary>
    /// <param name="id">The ID of the course to retrieve</param>
    /// <returns>The course with the specified ID, or null if not found</returns>
    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _courseRepository.GetCourseByIdAsync(id);
    }

    /// <summary>
    /// Creates a new course record
    /// </summary>
    /// <param name="createCourse">The course data to create</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>A tuple containing the created course (if successful) and an error message (if any)</returns>
    public async Task<(Course?, string?)> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user)
    {

        if (string.IsNullOrWhiteSpace(createCourse.Name))
        {
            return (null, "Course name is required");
        }

        var userId = await _userContextService.GetUserIdAsync(user);
        if (string.IsNullOrEmpty(userId))
        {
            return (null, "User ID not found in token");
        }

        var course = new Course
        {
            Name = createCourse.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdCourse = await _courseRepository.CreateCourse(course);
        await _courseRepository.SaveChangesAsync();

        return (createdCourse, null);
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
        // Additional validation for defense in depth
        if (updateCourse == null)
        {
            return (null, "Update course data is required");
        }

        var userId = await _userContextService.GetUserIdAsync(user);
        if (string.IsNullOrEmpty(userId))
        {
            return (null, "User ID not found in token");
        }

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            return (null, "Course not found");
        }

        if (!string.IsNullOrEmpty(updateCourse.Name))
        {
            existingCourse.Name = updateCourse.Name;
        }

        existingCourse.UpdatedAt = DateTime.UtcNow;

        var updatedCourse = await _courseRepository.UpdateCourseAsync(existingCourse);
        await _courseRepository.SaveChangesAsync();

        return (updatedCourse, null);
    }

    /// <summary>
    /// Deletes a course by ID
    /// </summary>
    /// <param name="id">The ID of the course to delete</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>An error message if deletion fails, null otherwise</returns>
    public async Task<string?> DeleteCourseAsync(int id, ClaimsPrincipal user)
    {
        var userId = await _userContextService.GetUserIdAsync(user);
        if (string.IsNullOrEmpty(userId))
        {
            return "User ID not found in token";
        }

        var existingCourse = await _courseRepository.GetCourseByIdAsync(id);
        if (existingCourse == null)
        {
            return "Course not found";
        }

        var result = await _courseRepository.DeleteCourseAsync(id);
        if (!result)
        {
            return "Failed to delete course";
        }

        await _courseRepository.SaveChangesAsync();
        return null;
    }
}