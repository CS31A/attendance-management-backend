using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing classroom-related operations
/// </summary>
public class ClassroomService : IClassroomService
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ILogger<ClassroomService> _logger;

    /// <summary>
    /// Initializes a new instance of the ClassroomService class
    /// </summary>
    /// <param name="classroomRepository">Repository for classroom data operations</param>
    /// <param name="logger">Logger for logging operations</param>
    public ClassroomService(IClassroomRepository classroomRepository, ILogger<ClassroomService> logger)
    {
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region GetAllClassroomsAsync
    /// <summary>
    /// Retrieves all classrooms
    /// </summary>
    /// <returns>A collection of classrooms</returns>
    public async Task<IEnumerable<Classroom>> GetAllClassroomsAsync()
    {
        _logger.LogInformation("Retrieving all classrooms");
        try
        {
            var classrooms = await _classroomRepository.GetAllClassroomsAsync().ConfigureAwait(false);
            var allClassrooms = classrooms.ToList();
            _logger.LogInformation("Successfully retrieved {Count} classrooms", allClassrooms.Count);
            return allClassrooms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all classrooms");
            throw new EntityServiceException("Classroom", "GetAllClassrooms", "An error occurred while retrieving classrooms", ex);
        }
    }
    #endregion

    #region GetClassroomByIdAsync
    /// <summary>
    /// Retrieves a specific classroom by ID
    /// </summary>
    /// <param name="id">The ID of the classroom to retrieve</param>
    /// <returns>The classroom with the specified ID, or null if not found</returns>
    public async Task<Classroom?> GetClassroomByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving classroom by ID: {Id}", id);
        try
        {
            var classroom = await _classroomRepository.GetClassroomByIdAsync(id).ConfigureAwait(false);
            if (classroom == null)
            {
                _logger.LogWarning("Classroom with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Classroom", id);
            }

            _logger.LogInformation("Successfully retrieved classroom with ID: {Id}", id);
            return classroom;
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving classroom with ID {Id}", id);
            throw new EntityServiceException("Classroom", $"GetClassroomById: {id}", "An error occurred while retrieving the classroom", ex);
        }
    }
    #endregion

    #region CreateClassroomAsync
    /// <summary>
    /// Creates a new classroom record
    /// </summary>
    /// <param name="createClassroom">The classroom data to create</param>
    /// <returns>A tuple containing the created classroom (if successful) and an error message (if any)</returns>
    public async Task<(Classroom?, string?)> CreateClassroomAsync(CreateClassroom createClassroom)
    {
        _logger.LogInformation("Creating new classroom with name: {ClassroomName}", createClassroom.Name);

        try
        {
            if (string.IsNullOrWhiteSpace(createClassroom.Name))
            {
                _logger.LogWarning("Classroom creation failed: Classroom name is required");
                return (null, "Classroom name is required");
            }

            // Check if a classroom with the same name already exists (first check)
            var existingClassroom = await _classroomRepository.GetClassroomByNameAsync(createClassroom.Name);
            if (existingClassroom != null)
            {
                _logger.LogWarning("Classroom creation failed: Classroom with name {Name} already exists", createClassroom.Name);
                return (null, $"A classroom with name {createClassroom.Name} already exists");
            }

            var classroom = new Classroom
            {
                Name = createClassroom.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var createdClassroom = await _classroomRepository.CreateClassroom(classroom).ConfigureAwait(false);
                await _classroomRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully created classroom with ID: {Id} and name: {ClassroomName}", createdClassroom.Id, createdClassroom.Name);
                return (createdClassroom, null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Classroom creation failed due to unique constraint violation: Classroom with name {Name} already exists", createClassroom.Name);
                return (null, $"A classroom with name {createClassroom.Name} already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating classroom with name: {ClassroomName}", createClassroom.Name);
            throw new EntityServiceException("Classroom", "CreateClassroom", "An error occurred while creating the classroom", ex);
        }
    }
    #endregion

    #region UpdateClassroomAsync
    /// <summary>
    /// Updates an existing classroom record
    /// </summary>
    /// <param name="id">The ID of the classroom to update</param>
    /// <param name="updateClassroom">The updated classroom data</param>
    /// <returns>A tuple containing the updated classroom (if successful) and an error message (if any)</returns>
    public async Task<(Classroom?, string?)> UpdateClassroomAsync(int id, UpdateClassroom updateClassroom)
    {
        _logger.LogInformation("Updating classroom with ID: {Id}", id);

        try
        {
            var existingClassroom = await _classroomRepository.GetClassroomByIdAsync(id).ConfigureAwait(false);
            if (existingClassroom == null)
            {
                _logger.LogWarning("Classroom update failed: Classroom with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Classroom", id);
            }

            // Check if the new name already exists for another classroom (first check)
            if (!string.IsNullOrEmpty(updateClassroom.Name) && !updateClassroom.Name.Equals(existingClassroom.Name))
            {
                var duplicateClassroom = await _classroomRepository.GetClassroomByNameAsync(updateClassroom.Name);
                if (duplicateClassroom != null && duplicateClassroom.Id != id)
                {
                    _logger.LogWarning("Classroom update failed: Classroom with name {Name} already exists", updateClassroom.Name);
                    return (null, $"A classroom with name {updateClassroom.Name} already exists");
                }
            }

            if (!string.IsNullOrEmpty(updateClassroom.Name))
            {
                existingClassroom.Name = updateClassroom.Name;
            }

            existingClassroom.UpdatedAt = DateTime.UtcNow;

            try
            {
                var updatedClassroom = await _classroomRepository.UpdateClassroomAsync(existingClassroom).ConfigureAwait(false);
                var rowsAffected = await _classroomRepository.SaveChangesAsync().ConfigureAwait(false);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Classroom update failed: Classroom with ID {Id} may have been updated by another process", id);
                    return (null, "Classroom may have been updated by another process. Please try again.");
                }

                _logger.LogInformation("Successfully updated classroom with ID: {Id}", id);
                return (updatedClassroom, null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning("Classroom update failed due to unique constraint violation: Classroom with name {Name} already exists", updateClassroom.Name);
                return (null, $"A classroom with name {updateClassroom.Name} already exists");
            }
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating classroom with ID {Id}", id);
            throw new EntityServiceException("Classroom", $"UpdateClassroom: {id}", "An error occurred while updating the classroom", ex);
        }
    }
    #endregion

    #region DeleteClassroomAsync
    /// <summary>
    /// Deletes a classroom by ID
    /// </summary>
    /// <param name="id">The ID of the classroom to delete</param>
    /// <returns>An error message if deletion fails, null otherwise</returns>
    public async Task<string?> DeleteClassroomAsync(int id)
    {
        _logger.LogInformation("Deleting classroom with ID: {Id}", id);

        try
        {
            var existingClassroom = await _classroomRepository.GetClassroomByIdAsync(id).ConfigureAwait(false);
            if (existingClassroom == null)
            {
                _logger.LogWarning("Classroom deletion failed: Classroom with ID {Id} not found", id);
                throw new EntityNotFoundException<int>("Classroom", id);
            }

            var result = await _classroomRepository.DeleteClassroomAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Classroom deletion failed: Failed to delete classroom with ID {Id}", id);
                return "Failed to delete classroom";
            }

            var rowsAffected = await _classroomRepository.SaveChangesAsync().ConfigureAwait(false);
            if (rowsAffected == 0)
            {
                _logger.LogWarning("Classroom deletion failed: Classroom with ID {Id} may have been deleted by another process", id);
                return "Classroom may have been deleted by another process.";
            }
            _logger.LogInformation("Successfully deleted classroom with ID: {Id}", id);
            return null;
        }
        catch (EntityNotFoundException<int>)
        {
            // Re-throw the specific exception for the controller to handle
            throw;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true
                                           || ex.InnerException?.Message.Contains("FK_") == true)
        {
            // Handle foreign key constraint violations with user-friendly messages
            var innerMessage = ex.InnerException?.Message ?? ex.Message;

            string userFriendlyMessage;
            if (innerMessage.Contains("FK_Schedules_Classrooms") || innerMessage.Contains("Schedules"))
            {
                userFriendlyMessage = "Cannot delete classroom because it is assigned to one or more schedules. Please remove the classroom from all schedules first.";
            }
            else
            {
                userFriendlyMessage = "Cannot delete classroom because it has associated records. Please remove all dependencies first.";
            }

            _logger.LogWarning(ex, "Classroom deletion failed due to foreign key constraint: {Message}", userFriendlyMessage);
            throw new EntityServiceException("Classroom", $"DeleteClassroom: {id}", userFriendlyMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting classroom with ID {Id}", id);
            throw new EntityServiceException("Classroom", $"DeleteClassroom: {id}", "An error occurred while deleting the classroom", ex);
        }
    }
    #endregion

    #region IsUniqueConstraintViolation
    /// <summary>
    /// Determines if a DbUpdateException is caused by a unique constraint violation
    /// </summary>
    /// <param name="ex">The DbUpdateException to check</param>
    /// <returns>True if the exception is caused by a unique constraint violation</returns>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Check if the inner exception message contains unique constraint violation indicators
        var innerException = ex.InnerException;
        if (innerException?.Message != null)
        {
            var message = innerException.Message.ToLower();
            return message.Contains("duplicate") ||
                   message.Contains("unique constraint") ||
                   message.Contains("constraint violation") ||
                   message.Contains("already exists");
        }
        return false;
    }
    #endregion
}