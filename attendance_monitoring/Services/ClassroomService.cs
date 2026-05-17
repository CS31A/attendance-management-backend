using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services.Crud;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing classroom-related operations.
/// Delegates CRUD operations to the generic CrudService; handles entity-specific
/// dependency checks via the classroom repository.
/// </summary>
public class ClassroomService : IClassroomService
{
    private readonly ICrudService<Classroom, CreateClassroom, UpdateClassroom> _crudService;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ILogger<ClassroomService> _logger;

    public ClassroomService(
        ICrudService<Classroom, CreateClassroom, UpdateClassroom> crudService,
        IClassroomRepository classroomRepository,
        ILogger<ClassroomService> logger)
    {
        _crudService = crudService;
        _classroomRepository = classroomRepository;
        _logger = logger;
    }

    #region CRUD Operations (delegated to CrudService)

    public async Task<IEnumerable<Classroom>> GetAllClassroomsAsync()
    {
        return await _crudService.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<Classroom?> GetClassroomByIdAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Classroom?> GetClassroomByUuidAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Classroom> CreateClassroomAsync(CreateClassroom createClassroom)
    {
        return await _crudService.CreateAsync(createClassroom).ConfigureAwait(false);
    }

    public async Task<Classroom> UpdateClassroomAsync(Guid id, UpdateClassroom updateClassroom)
    {
        return await _crudService.UpdateAsync(id, updateClassroom).ConfigureAwait(false);
    }

    public async Task<Classroom> UpdateClassroomByUuidAsync(Guid id, UpdateClassroom updateClassroom)
    {
        return await _crudService.UpdateAsync(id, updateClassroom).ConfigureAwait(false);
    }

    public async Task DeleteClassroomAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task DeleteClassroomByUuidAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    #endregion

    #region Dependency Check Operations (entity-specific)

    public async Task<bool> HasSchedulesInClassroomAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Checking if classroom {ClassroomId} has schedules", id);
            var hasSchedules = await _classroomRepository.HasSchedulesInClassroomAsync(id).ConfigureAwait(false);
            _logger.LogInformation("Classroom {ClassroomId} has schedules: {HasSchedules}", id, hasSchedules);
            return hasSchedules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if classroom {ClassroomId} has schedules", id);
            throw new EntityServiceException("Classroom", $"HasSchedulesInClassroom: {id}", "Error checking classroom dependencies", ex);
        }
    }

    public async Task<bool> HasSessionsInClassroomAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Checking if classroom {ClassroomId} has sessions", id);
            var hasSessions = await _classroomRepository.HasSessionsInClassroomAsync(id).ConfigureAwait(false);
            _logger.LogInformation("Classroom {ClassroomId} has sessions: {HasSessions}", id, hasSessions);
            return hasSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if classroom {ClassroomId} has sessions", id);
            throw new EntityServiceException("Classroom", $"HasSessionsInClassroom: {id}", "Error checking classroom dependencies", ex);
        }
    }

    #endregion
}
