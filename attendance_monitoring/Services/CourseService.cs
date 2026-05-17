using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services.Crud;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing course-related operations.
/// Delegates CRUD operations to the generic CrudService; handles entity-specific
/// user context validation and dependency checks via the course repository.
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICrudService<Course, CreateCourse, UpdateCourse> _crudService;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<CourseService> _logger;

    public CourseService(
        ICrudService<Course, CreateCourse, UpdateCourse> crudService,
        ICourseRepository courseRepository,
        IUserContextService userContextService,
        ILogger<CourseService> logger)
    {
        _crudService = crudService;
        _courseRepository = courseRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    #region Read Operations (delegated to CrudService)

    public async Task<IList<Course>> GetAllCoursesAsync()
    {
        var courses = await _crudService.GetAllAsync().ConfigureAwait(false);
        return courses.ToList();
    }

    public async Task<Course> GetCourseByIdAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Course> GetCourseByUuidAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    #endregion

    #region Mutation Operations (auth validation + CrudService delegation)

    public async Task<Course> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user)
    {
        await ValidateUserContextAsync(user, "CreateCourse", null).ConfigureAwait(false);
        return await _crudService.CreateAsync(createCourse).ConfigureAwait(false);
    }

    public async Task<Course> UpdateCourseAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user)
    {
        await ValidateUserContextAsync(user, "UpdateCourse", id).ConfigureAwait(false);
        return await _crudService.UpdateAsync(id, updateCourse).ConfigureAwait(false);
    }

    public async Task<Course> UpdateCourseByUuidAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user)
    {
        await ValidateUserContextAsync(user, "UpdateCourse", id).ConfigureAwait(false);
        return await _crudService.UpdateAsync(id, updateCourse).ConfigureAwait(false);
    }

    public async Task DeleteCourseAsync(Guid id, ClaimsPrincipal user)
    {
        await ValidateUserContextAsync(user, "DeleteCourse", id).ConfigureAwait(false);
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task DeleteCourseByUuidAsync(Guid id, ClaimsPrincipal user)
    {
        await ValidateUserContextAsync(user, "DeleteCourse", id).ConfigureAwait(false);
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    #endregion

    #region Dependency Check Operations (entity-specific)

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

    #region Helpers

    private async Task ValidateUserContextAsync(ClaimsPrincipal user, string operation, Guid? courseId)
    {
        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            var context = courseId.HasValue ? $" for course {courseId}" : "";
            _logger.LogWarning("{Operation} failed{Context}: User ID not found in token", operation, context);
            throw new EntityServiceException("Course", $"{operation}{context}", "User ID not found in token");
        }
    }

    #endregion
}
