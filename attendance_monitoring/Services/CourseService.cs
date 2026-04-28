using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing course-related operations
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// Initializes a new instance of the CourseService class
    /// </summary>
    /// <param name="courseRepository">Repository for course data operations</param>
    /// <param name="userContextService">Service for managing user context and authorization</param>
    /// <param name="logger">Logger for logging operations</param>
    public CourseService(ICourseRepository courseRepository, IUserContextService userContextService, ILogger<CourseService> logger)
    {
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Get Operations
    #region GetAllCoursesAsync
    /// <summary>
    /// Retrieves all courses
    /// </summary>
    /// <returns>A collection of courses</returns>
    public async Task<IList<Course>> GetAllCoursesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all courses");
            var courses = await _courseRepository.GetAllCoursesAsync().ConfigureAwait(false);
            var allCoursesAsync = courses.ToList();
            _logger.LogInformation("Successfully retrieved {Count} courses", allCoursesAsync.Count);
            return allCoursesAsync;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving courses");
            throw new EntityServiceException("Course", "GetAllCourses", "An error occurred while retrieving courses", ex);
        }
    }
    #endregion

    #region GetCourseByIdAsync
    /// <summary>
    /// Retrieves a specific course by ID
    /// </summary>
    /// <param name="id">The ID of the course to retrieve</param>
    /// <returns>The course with the specified ID</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the course is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<Course> GetCourseByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving course by ID: {Id}", id);
            var course = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
            if (course == null)
            {
                _logger.LogWarning("Course with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Course", id);
            }

            _logger.LogInformation("Successfully retrieved course with ID: {Id}", id);
            return course;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving course with ID: {Id}", id);
            throw new EntityServiceException("Course", $"GetCourseById: {id}", "An error occurred while retrieving the course", ex);
        }
    }
    #endregion

    #region GetCourseByUuidAsync
    public async Task<Course> GetCourseByUuidAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving course by UUID: {Id}", id);
            var course = await _courseRepository.GetCourseByUuidAsync(id).ConfigureAwait(false);
            if (course == null)
            {
                _logger.LogWarning("Course with UUID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Course", id);
            }

            _logger.LogInformation("Successfully retrieved course with UUID: {Id}", id);
            return course;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving course with UUID: {Id}", id);
            throw new EntityServiceException("Course", $"GetCourseByUuid: {id}", "An error occurred while retrieving the course", ex);
        }
    }
    #endregion

    #endregion

    #region Create Operations
    #region CreateCourseAsync
    /// <summary>
    /// Creates a new course record
    /// </summary>
    /// <param name="createCourse">The course data to create</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>The created course</returns>
    /// <exception cref="EntityServiceException">Thrown when course creation fails</exception>
    public async Task<Course> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Creating new course with name: {CourseName}", createCourse.Name);

            if (string.IsNullOrWhiteSpace(createCourse.Name))
            {
                _logger.LogWarning("Course creation failed: Course name is required");
                throw new EntityServiceException("Course", "CreateCourse", "Course name is required");
            }

            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Course creation failed: User ID not found in token");
                throw new EntityServiceException("Course", "CreateCourse", "User ID not found in token");
            }

            var course = new Course
            {
                Name = createCourse.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCourse = await _courseRepository.CreateCourse(course).ConfigureAwait(false);
            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created course with ID: {Id} and name: {CourseName}", createdCourse.Id, createdCourse.Name);
            return createdCourse;
        }
        catch (EntityServiceException)
        {
            // Re-throw EntityServiceException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating course with name: {CourseName}", createCourse.Name);
            throw new EntityServiceException("Course", "CreateCourse", "An error occurred while creating the course", ex);
        }
    }
    #endregion

    #endregion

    #region Update Operations
    #region UpdateCourseAsync
    /// <summary>
    /// Updates an existing course record
    /// </summary>
    /// <param name="id">The ID of the course to update</param>
    /// <param name="updateCourse">The updated course data</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>The updated course</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the course is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when course update fails</exception>
    public async Task<Course> UpdateCourseAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Updating course with ID: {Id}", id);

            // Additional validation for defense in depth
            if (updateCourse == null)
            {
                _logger.LogWarning("Course update failed: Update course data is required");
                throw new EntityServiceException("Course", $"UpdateCourse: {id}", "Update course data is required");
            }

            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Course update failed: User ID not found in token");
                throw new EntityServiceException("Course", $"UpdateCourse: {id}", "User ID not found in token");
            }

            var existingCourse = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
            if (existingCourse == null)
            {
                _logger.LogWarning("Course update failed: Course with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Course", id);
            }

            if (!string.IsNullOrEmpty(updateCourse.Name))
            {
                existingCourse.Name = updateCourse.Name;
            }

            existingCourse.UpdatedAt = DateTime.UtcNow;

            var updatedCourse = await _courseRepository.UpdateCourseAsync(existingCourse).ConfigureAwait(false);
            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated course with ID: {Id}", id);
            return updatedCourse;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (EntityServiceException)
        {
            // Re-throw EntityServiceException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating course with ID: {Id}", id);
            throw new EntityServiceException("Course", $"UpdateCourse: {id}", "An error occurred while updating the course", ex);
        }
    }
    #endregion

    #region UpdateCourseByUuidAsync
    public async Task<Course> UpdateCourseByUuidAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user)
    {
        var existingCourse = await GetCourseByUuidAsync(id).ConfigureAwait(false);
        return await UpdateCourseAsync(existingCourse.Id, updateCourse, user).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Delete Operations
    #region DeleteCourseAsync
    /// <summary>
    /// Deletes a course by ID
    /// </summary>
    /// <param name="id">The ID of the course to delete</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the course is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when course deletion fails</exception>
    public async Task DeleteCourseAsync(Guid id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deleting course with ID: {Id}", id);

            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Course deletion failed: User ID not found in token");
                throw new EntityServiceException("Course", $"DeleteCourse: {id}", "User ID not found in token");
            }

            var existingCourse = await _courseRepository.GetCourseByIdAsync(id).ConfigureAwait(false);
            if (existingCourse == null)
            {
                _logger.LogWarning("Course deletion failed: Course with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Course", id);
            }

            var result = await _courseRepository.DeleteCourseAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Course deletion failed: Failed to delete course with ID {Id}", id);
                throw new EntityServiceException("Course", $"DeleteCourse: {id}", "Failed to delete course");
            }

            await _courseRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted course with ID: {Id}", id);
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (EntityServiceException)
        {
            // Re-throw EntityServiceException as-is
            throw;
        }
        catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsForeignKeyViolation(ex))
        {
            var conflictMessage = ResolveDeleteConflictMessage(ex);
            _logger.LogWarning(ex, "Course deletion failed due to foreign key constraint: {Message}", conflictMessage);
            throw new EntityConflictException("Course", "sections", conflictMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting course with ID: {Id}", id);
            throw new EntityServiceException("Course", $"DeleteCourse: {id}", "An error occurred while deleting the course", ex);
        }
    }
    #endregion

    #region DeleteCourseByUuidAsync
    public async Task DeleteCourseByUuidAsync(Guid id, ClaimsPrincipal user)
    {
        var existingCourse = await GetCourseByUuidAsync(id).ConfigureAwait(false);
        await DeleteCourseAsync(existingCourse.Id, user).ConfigureAwait(false);
    }
    #endregion

    #region Dependency Check Operations
    public async Task<bool> HasSectionsInCourseAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Checking if course {CourseId} has sections", id);
            var hasSections = await _courseRepository.HasSectionsInCourseAsync(id).ConfigureAwait(false);
            _logger.LogInformation("Course {CourseId} has sections: {HasSections}", id, hasSections);
            return hasSections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if course {CourseId} has sections", id);
            throw new EntityServiceException("Course", $"HasSectionsInCourse: {id}", "Error checking course dependencies", ex);
        }
    }
    #endregion

    private static string ResolveDeleteConflictMessage(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("FK_Sections_Courses", StringComparison.OrdinalIgnoreCase) || message.Contains("Sections", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot delete: Course has sections assigned. Remove sections first.";
        }

        return "Cannot delete: Course has dependencies that prevent deletion.";
    }

    #endregion
}
