using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing subject-related operations
/// </summary>
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly UserContextService _userContextService;
    private readonly ILogger<SubjectService> _logger;

    /// <summary>
    /// Initializes a new instance of the SubjectService class
    /// </summary>
    /// <param name="subjectRepository">Repository for subject data operations</param>
    /// <param name="userContextService">Service for managing user context and authorization</param>
    /// <param name="logger">Logger for logging operations</param>
    public SubjectService(ISubjectRepository subjectRepository, UserContextService userContextService, ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository ?? throw new ArgumentNullException(nameof(subjectRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all subjects
    /// </summary>
    /// <returns>A collection of subjects</returns>
    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        _logger.LogInformation("Retrieving all subjects");
        var subjects = await _subjectRepository.GetAllSubjectsAsync().ConfigureAwait(false);
        var allSubjects = subjects.ToList();
        _logger.LogInformation("Successfully retrieved {Count} subjects", allSubjects.Count);
        return allSubjects;
    }

    /// <summary>
    /// Retrieves a specific subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to retrieve</param>
    /// <returns>The subject with the specified ID, or null if not found</returns>
    public async Task<Subject?> GetSubjectByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving subject by ID: {Id}", id);
        var subject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
        if (subject == null)
        {
            _logger.LogWarning("Subject with ID {Id} not found", id);
        }
        else
        {
            _logger.LogInformation("Successfully retrieved subject with ID: {Id}", id);
        }
        return subject;
    }

    /// <summary>
    /// Creates a new subject record
    /// </summary>
    /// <param name="createSubject">The subject data to create</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>A tuple containing the created subject (if successful) and an error message (if any)</returns>
    public async Task<(Subject?, string?)> CreateSubjectAsync(CreateSubject createSubject, ClaimsPrincipal user)
    {
        _logger.LogInformation("Creating new subject with name: {SubjectName}", createSubject.Name);

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

        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Subject creation failed: User ID not found in token");
            return (null, "User ID not found in token");
        }

        var subject = new Subject
        {
            Name = createSubject.Name,
            Code = createSubject.Code,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdSubject = await _subjectRepository.CreateSubject(subject).ConfigureAwait(false);
        await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Successfully created subject with ID: {Id} and name: {SubjectName}", createdSubject.Id, createdSubject.Name);
        return (createdSubject, null);
    }

    /// <summary>
    /// Updates an existing subject record
    /// </summary>
    /// <param name="id">The ID of the subject to update</param>
    /// <param name="updateSubject">The updated subject data</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>A tuple containing the updated subject (if successful) and an error message (if any)</returns>
    public async Task<(Subject?, string?)> UpdateSubjectAsync(int id, UpdateSubject updateSubject, ClaimsPrincipal user)
    {
        _logger.LogInformation("Updating subject with ID: {Id}", id);
        
        // Additional validation for defense in depth
        if (updateSubject == null)
        {
            _logger.LogWarning("Subject update failed: Update subject data is required");
            return (null, "Update subject data is required");
        }

        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Subject update failed: User ID not found in token");
            return (null, "User ID not found in token");
        }

        var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
        if (existingSubject == null)
        {
            _logger.LogWarning("Subject update failed: Subject with ID {Id} not found", id);
            return (null, "Subject not found");
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

        var updatedSubject = await _subjectRepository.UpdateSubjectAsync(existingSubject).ConfigureAwait(false);
        await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Successfully updated subject with ID: {Id}", id);
        return (updatedSubject, null);
    }

    /// <summary>
    /// Deletes a subject by ID
    /// </summary>
    /// <param name="id">The ID of the subject to delete</param>
    /// <param name="user">The claims principal of the current user</param>
    /// <returns>An error message if deletion fails, null otherwise</returns>
    public async Task<string?> DeleteSubjectAsync(int id, ClaimsPrincipal user)
    {
        _logger.LogInformation("Deleting subject with ID: {Id}", id);
        
        var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Subject deletion failed: User ID not found in token");
            return "User ID not found in token";
        }

        var existingSubject = await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false);
        if (existingSubject == null)
        {
            _logger.LogWarning("Subject deletion failed: Subject with ID {Id} not found", id);
            return "Subject not found";
        }

        var result = await _subjectRepository.DeleteSubjectAsync(id).ConfigureAwait(false);
        if (!result)
        {
            _logger.LogError("Subject deletion failed: Failed to delete subject with ID {Id}", id);
            return "Failed to delete subject";
        }

        await _subjectRepository.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogInformation("Successfully deleted subject with ID: {Id}", id);
        return null;
    }
}