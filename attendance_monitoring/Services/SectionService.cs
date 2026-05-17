using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.Crud;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing section-related operations.
/// Delegates CRUD operations to the generic CrudService; handles entity-specific
/// DTO mapping, student queries, and dependency checks via the section repository.
/// </summary>
public class SectionService : ISectionService
{
    private readonly ICrudService<Section, Section, Section> _crudService;
    private readonly ISectionRepository _sectionRepository;
    private readonly ILogger<SectionService> _logger;

    public SectionService(
        ICrudService<Section, Section, Section> crudService,
        ISectionRepository sectionRepository,
        ILogger<SectionService> logger)
    {
        _crudService = crudService;
        _sectionRepository = sectionRepository;
        _logger = logger;
    }

    #region Read Operations

    public async Task<Section> GetSectionByIdAsync(Guid sectionId)
    {
        try
        {
            var section = await _sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
            if (section == null)
                throw new EntityNotFoundException<Guid>("Section", sectionId);
            return section;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId} from repository.", sectionId);
            throw new EntityServiceException("Section", $"GetSectionById: {sectionId}", "An error occurred while retrieving the section", ex);
        }
    }

    public async Task<Section> GetSectionByUuidAsync(Guid id)
    {
        try
        {
            var section = await _sectionRepository.GetSectionByUuidAsync(id).ConfigureAwait(false);
            if (section == null)
                throw new EntityNotFoundException<Guid>("Section", id);
            return section;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving section with UUID {SectionId} from repository.", id);
            throw new EntityServiceException("Section", $"GetSectionByUuid: {id}", "An error occurred while retrieving the section", ex);
        }
    }

    public async Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all sections");
            var sections = await _sectionRepository.GetAllSectionsAsync().ConfigureAwait(false);
            var sectionDtos = sections.Select(MapToDto).ToList();
            _logger.LogInformation("Successfully retrieved {Count} sections", sectionDtos.Count);
            return sectionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all sections from repository.");
            throw new EntityServiceException("Section", "GetAllSections", "An error occurred while retrieving sections", ex);
        }
    }

    #endregion

    #region Create Operations

    public async Task<SectionResponseDto> CreateSectionAsync(Section section)
    {
        var created = await _crudService.CreateAsync(section).ConfigureAwait(false);
        return await RefreshAndMapAsync(created.Id, "CreateSection").ConfigureAwait(false);
    }

    #endregion

    #region Update Operations

    public async Task<SectionResponseDto> UpdateSectionAsync(Guid id, Section section)
    {
        var updated = await _crudService.UpdateAsync(id, section).ConfigureAwait(false);
        return await RefreshAndMapAsync(updated.Id, "UpdateSection").ConfigureAwait(false);
    }

    public async Task<SectionResponseDto> UpdateSectionByUuidAsync(Guid id, Section section)
    {
        var updated = await _crudService.UpdateAsync(id, section).ConfigureAwait(false);
        return await RefreshAndMapAsync(updated.Id, "UpdateSection").ConfigureAwait(false);
    }

    #endregion

    #region Delete Operations

    public async Task DeleteSectionAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task DeleteSectionByUuidAsync(Guid id)
    {
        await _crudService.DeleteAsync(id).ConfigureAwait(false);
    }

    #endregion

    #region Student Operations (entity-specific)

    public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(Guid sectionId)
    {
        try
        {
            var students = await _sectionRepository.GetActiveStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
            return students;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving active students for section {SectionId}.", sectionId);
            throw new EntityServiceException("Section", $"GetActiveStudentsBySectionId: {sectionId}", "An error occurred while retrieving active students", ex);
        }
    }

    public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(Guid sectionId)
    {
        try
        {
            var students = await _sectionRepository.GetAllStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
            return students;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving students for section {SectionId}.", sectionId);
            throw new EntityServiceException("Section", $"GetAllStudentsBySectionId: {sectionId}", "An error occurred while retrieving students", ex);
        }
    }

    #endregion

    #region Dependency Check Operations (entity-specific)

    public async Task<bool> HasStudentsInSectionAsync(Guid sectionId)
    {
        try
        {
            _logger.LogInformation("Checking if section {SectionId} has students", sectionId);
            var hasStudents = await _sectionRepository.HasStudentsInSectionAsync(sectionId).ConfigureAwait(false);
            _logger.LogInformation("Section {SectionId} has students: {HasStudents}", sectionId, hasStudents);
            return hasStudents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if section {SectionId} has students", sectionId);
            throw new EntityServiceException("Section", $"HasStudentsInSection: {sectionId}", "Error checking section dependencies", ex);
        }
    }

    public async Task<bool> HasStudentEnrollmentsInSectionAsync(Guid sectionId)
    {
        try
        {
            _logger.LogInformation("Checking if section {SectionId} has student enrollments", sectionId);
            var hasEnrollments = await _sectionRepository.HasStudentEnrollmentsInSectionAsync(sectionId).ConfigureAwait(false);
            _logger.LogInformation("Section {SectionId} has student enrollments: {HasEnrollments}", sectionId, hasEnrollments);
            return hasEnrollments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if section {SectionId} has student enrollments", sectionId);
            throw new EntityServiceException("Section", $"HasStudentEnrollmentsInSection: {sectionId}", "Error checking section dependencies", ex);
        }
    }

    public async Task<bool> HasSchedulesInSectionAsync(Guid sectionId)
    {
        try
        {
            _logger.LogInformation("Checking if section {SectionId} has schedules", sectionId);
            var hasSchedules = await _sectionRepository.HasSchedulesInSectionAsync(sectionId).ConfigureAwait(false);
            _logger.LogInformation("Section {SectionId} has schedules: {HasSchedules}", sectionId, hasSchedules);
            return hasSchedules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if section {SectionId} has schedules", sectionId);
            throw new EntityServiceException("Section", $"HasSchedulesInSection: {sectionId}", "Error checking section dependencies", ex);
        }
    }

    #endregion

    #region Helpers

    private async Task<SectionResponseDto> RefreshAndMapAsync(Guid sectionId, string operation)
    {
        var refreshed = await _sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        if (refreshed == null)
        {
            _logger.LogError("{Operation} succeeded but section {SectionId} could not be reloaded.", operation, sectionId);
            throw new InvalidOperationException($"{operation} succeeded but section could not be reloaded.");
        }
        return MapToDto(refreshed);
    }

    private static SectionResponseDto MapToDto(Section s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        CourseId = s.Course?.Id,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    #endregion
}
