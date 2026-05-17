using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services.Crud;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing subject-related operations.
/// Delegates CRUD operations to the generic CrudService; handles entity-specific
/// dependency checks via the subject repository.
/// </summary>
public class SubjectService : ISubjectService
{
    private readonly ICrudService<Subject, CreateSubject, UpdateSubject> _crudService;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(
        ICrudService<Subject, CreateSubject, UpdateSubject> crudService,
        ISubjectRepository subjectRepository,
        ILogger<SubjectService> logger)
    {
        _crudService = crudService;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    #region CRUD Operations (delegated to CrudService)

    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        return await _crudService.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<Subject?> GetSubjectByIdAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Subject?> GetSubjectByUuidAsync(Guid id)
    {
        return await _crudService.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Subject> CreateSubjectAsync(CreateSubject createSubject)
    {
        return await _crudService.CreateAsync(createSubject).ConfigureAwait(false);
    }

    public async Task<Subject> UpdateSubjectAsync(Guid id, UpdateSubject updateSubject)
    {
        return await _crudService.UpdateAsync(id, updateSubject).ConfigureAwait(false);
    }

    public async Task<Subject> UpdateSubjectByUuidAsync(Guid id, UpdateSubject updateSubject)
    {
        return await _crudService.UpdateAsync(id, updateSubject).ConfigureAwait(false);
    }

    public async Task DeleteSubjectAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task DeleteSubjectByUuidAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    #endregion

    #region Dependency Check Operations (entity-specific)

    public async Task<bool> HasSchedulesInSubjectAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Checking if subject {SubjectId} has schedules", id);
            var hasSchedules = await _subjectRepository.HasSchedulesInSubjectAsync(id).ConfigureAwait(false);
            _logger.LogInformation("Subject {SubjectId} has schedules: {HasSchedules}", id, hasSchedules);
            return hasSchedules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if subject {SubjectId} has schedules", id);
            throw new EntityServiceException("Subject", $"HasSchedulesInSubject: {id}", "Error checking subject dependencies", ex);
        }
    }

    public async Task<bool> HasEnrollmentsInSubjectAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Checking if subject {SubjectId} has enrollments", id);
            var hasEnrollments = await _subjectRepository.HasEnrollmentsInSubjectAsync(id).ConfigureAwait(false);
            _logger.LogInformation("Subject {SubjectId} has enrollments: {HasEnrollments}", id, hasEnrollments);
            return hasEnrollments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if subject {SubjectId} has enrollments", id);
            throw new EntityServiceException("Subject", $"HasEnrollmentsInSubject: {id}", "Error checking subject dependencies", ex);
        }
    }

    #endregion
}
