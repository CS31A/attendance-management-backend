using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing subject-related operations
/// </summary>
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<SubjectService> _logger;

    /// <summary>
    /// Initializes a new instance of the SubjectService class
    /// </summary>
    /// <param name="subjectRepository">Repository for subject data operations</param>
    /// <param name="logger">Logger for logging operations</param>
    public SubjectService(ISubjectRepository subjectRepository, ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository ?? throw new ArgumentNullException(nameof(subjectRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Get Operations

    /// <summary>
    /// Retrieves all subjects
    /// </summary>
    /// <returns>A collection of subjects</returns>
    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        _logger.LogInformation("Retrieving all subjects");
        try
        {
            var subjects = await _subjectRepository.GetAllSubjectsAsync().ConfigureAwait(false);
            var allSubjects = subjects.ToList();
            _logger.LogInformation("Successfully retrieved {Count} subjects", allSubjects.Count);
            return allSubjects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all subjects");
            throw new EntityServiceException("Subject", "GetAllSubjects", "An error occurred while retrieving subjects", ex);
        }
    }

    /// <summary>
    /// Retrieves a specific subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to retrieve</param>
    /// <returns>The subject with the specified ID, or null if not found</returns>
    public async Task<Subject?> GetSubjectByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving subject by ID: {Id}", id);
        try
        {
            var subject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
            if (subject == null)
            {
                _logger.LogWarning("Subject with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Subject", id);
            }

            _logger.LogInformation("Successfully retrieved subject with ID: {Id}", id);
            return subject;
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving subject with ID {Id}", id);
            throw new EntityServiceException("Subject", $"GetSubjectById: {id}", "An error occurred while retrieving the subject", ex);
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Creates a new subject record
    /// </summary>
    /// <param name="createSubject">The subject data to create</param>
    /// <returns>The created subject</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityAlreadyExistsException{TKey}">Thrown when subject with code already exists</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during creation</exception>
    public async Task<Subject> CreateSubjectAsync(CreateSubject createSubject)
    {
        _logger.LogInformation("Creating new subject with name: {SubjectName}", createSubject.Name);

        try
        {
            if (string.IsNullOrWhiteSpace(createSubject.Name))
            {
                _logger.LogWarning("Subject creation failed: Subject name is required");
                throw new ValidationException("Subject name is required");
            }

            if (string.IsNullOrWhiteSpace(createSubject.Code))
            {
                _logger.LogWarning("Subject creation failed: Subject code is required");
                throw new ValidationException("Subject code is required");
            }

            // Check if a subject with the same code already exists (first check)
            var existingSubject = await _subjectRepository.GetSubjectByCodeAsync(createSubject.Code);
            if (existingSubject != null)
            {
                _logger.LogWarning("Subject creation failed: Subject with code {Code} already exists", createSubject.Code);
                throw new EntityAlreadyExistsException<string>("Subject", "Code", createSubject.Code);
            }

            var subject = new Subject
            {
                Name = createSubject.Name,
                Code = createSubject.Code,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var createdSubject = await _subjectRepository.CreateSubject(subject).ConfigureAwait(false);
                await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully created subject with ID: {Id} and name: {SubjectName}", createdSubject.Id, createdSubject.Name);
                return createdSubject;
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Subject creation failed due to unique constraint violation: Subject with code {Code} already exists", createSubject.Code);
                throw new EntityAlreadyExistsException<string>("Subject", "Code", createSubject.Code);
            }
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityAlreadyExistsException<string>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating subject with name: {SubjectName}", createSubject.Name);
            throw ExceptionHandlingHelper.CreateServiceException("Subject", "CreateSubject", ex);
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates an existing subject record
    /// </summary>
    /// <param name="id">The ID of the subject to update</param>
    /// <param name="updateSubject">The updated subject data</param>
    /// <returns>The updated subject</returns>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when subject not found</exception>
    /// <exception cref="EntityAlreadyExistsException{TKey}">Thrown when subject with code already exists</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during update</exception>
    public async Task<Subject> UpdateSubjectAsync(int id, UpdateSubject updateSubject)
    {
        _logger.LogInformation("Updating subject with ID: {Id}", id);

        try
        {

            var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
            if (existingSubject == null)
            {
                _logger.LogWarning("Subject update failed: Subject with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Subject", id);
            }

            // Check if the new code already exists for another subject (first check)
            if (!string.IsNullOrEmpty(updateSubject.Code) && !updateSubject.Code.Equals(existingSubject.Code))
            {
                var duplicateSubject = await _subjectRepository.GetSubjectByCodeAsync(updateSubject.Code);
                if (duplicateSubject != null && duplicateSubject.Id != id)
                {
                    _logger.LogWarning("Subject update failed: Subject with code {Code} already exists", updateSubject.Code);
                    throw new EntityAlreadyExistsException<string>("Subject", "Code", updateSubject.Code);
                }
            }

            if (!string.IsNullOrEmpty(updateSubject.Name))
            {
                existingSubject.Name = updateSubject.Name;
            }

            if (!string.IsNullOrEmpty(updateSubject.Code))
            {
                existingSubject.Code = updateSubject.Code;
            }

            existingSubject.UpdatedAt = DateTime.UtcNow;

            try
            {
                var updatedSubject = await _subjectRepository.UpdateSubjectAsync(existingSubject).ConfigureAwait(false);
                var rowsAffected = await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Subject update failed: Subject with ID {Id} may have been updated by another process", id);
                    throw new EntityServiceException("Subject", $"UpdateSubject: {id}", "Subject may have been updated by another process. Please try again.");
                }

                _logger.LogInformation("Successfully updated subject with ID: {Id}", id);
                return updatedSubject;
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Subject update failed due to unique constraint violation: Subject with code {Code} already exists", updateSubject.Code);
                throw new EntityAlreadyExistsException<string>("Subject", "Code", updateSubject.Code ?? "");
            }
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (EntityAlreadyExistsException<string>)
        {
            throw;
        }
        catch (EntityServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating subject with ID {Id}", id);
            throw ExceptionHandlingHelper.CreateServiceException("Subject", $"UpdateSubject: {id}", ex);
        }
    }

    #endregion

    #region Dependency Check Operations
    public async Task<bool> HasSchedulesInSubjectAsync(int id)
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

    public async Task<bool> HasEnrollmentsInSubjectAsync(int id)
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

    #region Delete Operations

    /// <summary>
    /// Deletes a subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to delete</param>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when subject not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during deletion</exception>
    public async Task DeleteSubjectAsync(int id)
    {
        _logger.LogInformation("Deleting subject with ID: {Id}", id);

        try
        {
            var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
            if (existingSubject == null)
            {
                _logger.LogWarning("Subject deletion failed: Subject with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Subject", id);
            }

            var result = await _subjectRepository.DeleteSubjectAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Subject deletion failed: Failed to delete subject with ID {Id}", id);
                throw new EntityServiceException("Subject", $"DeleteSubject: {id}", "Failed to delete subject");
            }

            var rowsAffected = await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);
            if (rowsAffected == 0)
            {
                _logger.LogWarning("Subject deletion failed: Subject with ID {Id} may have been deleted by another process", id);
                throw new EntityServiceException("Subject", $"DeleteSubject: {id}", "Subject may have been deleted by another process.");
            }
            _logger.LogInformation("Successfully deleted subject with ID: {Id}", id);
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (EntityServiceException)
        {
            throw;
        }
        catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsForeignKeyViolation(ex))
        {
            var conflictType = ResolveDeleteConflictType(ex);
            var conflictMessage = ResolveDeleteConflictMessage(ex);
            _logger.LogWarning(ex, "Subject deletion failed due to foreign key constraint: {Message}", conflictMessage);
            throw new EntityConflictException("Subject", conflictType, conflictMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting subject with ID {Id}", id);
            throw ExceptionHandlingHelper.CreateServiceException("Subject", $"DeleteSubject: {id}", ex);
        }
    }

    #endregion

    private static string ResolveDeleteConflictType(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("FK_StudentEnrollments", StringComparison.OrdinalIgnoreCase) || message.Contains("StudentEnrollments", StringComparison.OrdinalIgnoreCase))
        {
            return "enrollments";
        }

        if (message.Contains("FK_Schedules_Subjects", StringComparison.OrdinalIgnoreCase) || message.Contains("Schedules", StringComparison.OrdinalIgnoreCase))
        {
            return "schedules";
        }

        return "dependencies";
    }

    private static string ResolveDeleteConflictMessage(DbUpdateException ex)
    {
        return ResolveDeleteConflictType(ex) switch
        {
            "schedules" => "Cannot delete: Subject has schedules assigned. Remove schedules first.",
            "enrollments" => "Cannot delete: Subject has student enrollments. Remove enrollments first.",
            _ => "Cannot delete: Subject has dependencies that prevent deletion.",
        };
    }
}
