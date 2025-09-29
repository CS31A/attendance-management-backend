using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
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
            throw new SubjectServiceException("GetAllSubjects", "An error occurred while retrieving subjects", ex);
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
                throw new SubjectNotFoundException(id);
            }

            _logger.LogInformation("Successfully retrieved subject with ID: {Id}", id);
            return subject;
        }
        catch (SubjectNotFoundException)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving subject with ID {Id}", id);
            throw new SubjectServiceException($"GetSubjectById: {id}", "An error occurred while retrieving the subject", ex);
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Creates a new subject record
    /// </summary>
    /// <param name="createSubject">The subject data to create</param>
    /// <returns>A tuple containing the created subject (if successful) and an error message (if any)</returns>
    public async Task<(Subject?, string?)> CreateSubjectAsync(CreateSubject createSubject)
    {
        _logger.LogInformation("Creating new subject with name: {SubjectName}", createSubject.Name);

        try
        {
            if (string.IsNullOrWhiteSpace(createSubject.Name))
            {
                _logger.LogWarning("Subject creation failed: Subject name is required");
                return (null, "Subject name is required");
            }

            if (string.IsNullOrWhiteSpace(createSubject.Code))
            {
                _logger.LogWarning("Subject creation failed: Subject code is required");
                return (null, "Subject code is required");
            }

            // Check if a subject with the same code already exists (first check)
            var existingSubject = await _subjectRepository.GetSubjectByCodeAsync(createSubject.Code);
            if (existingSubject != null)
            {
                _logger.LogWarning("Subject creation failed: Subject with code {Code} already exists", createSubject.Code);
                return (null, $"A subject with code {createSubject.Code} already exists");
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
                return (createdSubject, null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Subject creation failed due to unique constraint violation: Subject with code {Code} already exists", createSubject.Code);
                return (null, $"A subject with code {createSubject.Code} already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating subject with name: {SubjectName}", createSubject.Name);
            throw new SubjectServiceException("CreateSubject", "An error occurred while creating the subject", ex);
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates an existing subject record
    /// </summary>
    /// <param name="id">The ID of the subject to update</param>
    /// <param name="updateSubject">The updated subject data</param>
    /// <returns>A tuple containing the updated subject (if successful) and an error message (if any)</returns>
    public async Task<(Subject?, string?)> UpdateSubjectAsync(int id, UpdateSubject updateSubject)
    {
        _logger.LogInformation("Updating subject with ID: {Id}", id);
        
        try
        {

            var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
            if (existingSubject == null)
            {
                _logger.LogWarning("Subject update failed: Subject with ID {Id} not found", id);
                throw new SubjectNotFoundException(id);
            }

            // Check if the new code already exists for another subject (first check)
            if (!string.IsNullOrEmpty(updateSubject.Code) && !updateSubject.Code.Equals(existingSubject.Code))
            {
                var duplicateSubject = await _subjectRepository.GetSubjectByCodeAsync(updateSubject.Code);
                if (duplicateSubject != null && duplicateSubject.Id != id)
                {
                    _logger.LogWarning("Subject update failed: Subject with code {Code} already exists", updateSubject.Code);
                    return (null, $"A subject with code {updateSubject.Code} already exists");
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
                    return (null, "Subject may have been updated by another process. Please try again.");
                }

                _logger.LogInformation("Successfully updated subject with ID: {Id}", id);
                return (updatedSubject, null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Subject update failed due to unique constraint violation: Subject with code {Code} already exists", updateSubject.Code);
                return (null, $"A subject with code {updateSubject.Code} already exists");
            }
        }
        catch (SubjectNotFoundException)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating subject with ID {Id}", id);
            throw new SubjectServiceException($"UpdateSubject: {id}", "An error occurred while updating the subject", ex);
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Deletes a subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to delete</param>
    /// <returns>An error message if deletion fails, null otherwise</returns>
    public async Task<string?> DeleteSubjectAsync(int id)
    {
        _logger.LogInformation("Deleting subject with ID: {Id}", id);
        
        try
        {
            var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
            if (existingSubject == null)
            {
                _logger.LogWarning("Subject deletion failed: Subject with ID {Id} not found", id);
                throw new SubjectNotFoundException(id);
            }

            var result = await _subjectRepository.DeleteSubjectAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Subject deletion failed: Failed to delete subject with ID {Id}", id);
                return "Failed to delete subject";
            }

            var rowsAffected = await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);
            if (rowsAffected == 0)
            {
                _logger.LogWarning("Subject deletion failed: Subject with ID {Id} may have been deleted by another process", id);
                return "Subject may have been deleted by another process.";
            }
            _logger.LogInformation("Successfully deleted subject with ID: {Id}", id);
            return null;
        }
        catch (SubjectNotFoundException)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting subject with ID {Id}", id);
            throw new SubjectServiceException($"DeleteSubject: {id}", "An error occurred while deleting the subject", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Determines if a DbUpdateException is caused by a unique constraint violation
    /// </summary>
    /// <param name="ex">The DbUpdateException to check</param>
    /// <returns>True if the exception is caused by a unique constraint violation</returns>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Check for SQL Server unique constraint violations
        var innerException = ex.InnerException;
        if (innerException == null) return false;
        var message = innerException.Message.ToLowerInvariant();
        
        return message.Contains("unique constraint") ||
               message.Contains("duplicate key") ||
               message.Contains("cannot insert duplicate key") ||
               message.Contains("unique index") ||
               message.Contains("violation of unique key constraint");

    }

    #endregion
}