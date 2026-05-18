using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.Services.InstructorServices;

/// <summary>
/// Service for instructor CRUD operations with authorization
/// </summary>
public class InstructorCrudService : IInstructorCrudService
{
    private readonly IInstructorRepository _instructorRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<InstructorCrudService> _logger;

    public InstructorCrudService(
        IInstructorRepository instructorRepository,
        IUserContextService userContextService,
        ILogger<InstructorCrudService> logger)
    {
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region GetAllInstructorsAsync
    /// <summary>
    /// Retrieves all instructors
    /// </summary>
    /// <returns>A collection of instructors</returns>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<IList<Instructor>> GetAllInstructorsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all instructors");
            var instructors = await _instructorRepository.GetAllInstructorsAsync().ConfigureAwait(false);
            var allInstructorsAsync = instructors.ToList();
            _logger.LogInformation("Successfully retrieved {Count} instructors", allInstructorsAsync.Count);
            return allInstructorsAsync;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving instructors");
            throw new EntityServiceException("Instructor", "GetAllInstructors", "An error occurred while retrieving instructors", ex);
        }
    }
    #endregion

    #region GetInstructorByIdAsync
    /// <summary>
    /// Retrieves a specific instructor by ID
    /// </summary>
    /// <param name="id">The ID of the instructor to retrieve</param>
    /// <returns>The instructor with the specified ID</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<Instructor> GetInstructorByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving instructor by ID: {Id}", id);
            var instructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("Instructor with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            _logger.LogInformation("Successfully retrieved instructor with ID: {Id}", id);
            return instructor;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving instructor with ID: {Id}", id);
            throw new EntityServiceException("Instructor", $"GetInstructorById: {id}", "An error occurred while retrieving the instructor", ex);
        }
    }
    #endregion

    #region CreateInstructorAsync
    /// <summary>
    /// Creates a new instructor record
    /// </summary>
    /// <param name="createInstructor">The instructor data to create</param>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>The created instructor</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityAlreadyExistsException{TKey}">Thrown when instructor already exists for user</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during creation</exception>
    public async Task<Instructor> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal userPrincipal)
    {
        _logger.LogInformation("Creating new instructor with name: {FirstName} {LastName}",
            createInstructor.Firstname, createInstructor.Lastname);

        try
        {
            // Validate basic user information
            if (string.IsNullOrWhiteSpace(createInstructor.Firstname) ||
                string.IsNullOrWhiteSpace(createInstructor.Lastname))
            {
                _logger.LogWarning("Instructor creation failed: First name and last name are required");
                throw new ValidationException("First name and last name are required");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor creation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            // Check if instructor already exists for this user
            var existingInstructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (existingInstructor != null)
            {
                _logger.LogWarning("Instructor creation failed: An instructor record already exists for this user");
                throw new EntityAlreadyExistsException<string>("Instructor", "UserId", userId);
            }

            var instructor = new Instructor
            {
                Firstname = createInstructor.Firstname,
                Lastname = createInstructor.Lastname,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdInstructor = await _instructorRepository.CreateInstructorAsync(instructor).ConfigureAwait(false);
            await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created instructor with ID: {Id} and name: {FirstName} {LastName}",
                createdInstructor.Id, createdInstructor.Firstname, createdInstructor.Lastname);
            return createdInstructor;
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
            _logger.LogError(ex, "Error occurred while creating instructor with name: {FirstName} {LastName}",
                createInstructor.Firstname, createInstructor.Lastname);
            throw ExceptionHandlingHelper.CreateServiceException("Instructor", "CreateInstructor", ex);
        }
    }
    #endregion

    #region UpdateInstructorAsync
    /// <summary>
    /// Updates an existing instructor record
    /// </summary>
    /// <param name="id">The ID of the instructor to update</param>
    /// <param name="updateInstructor">The updated instructor data</param>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>The updated instructor</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to update the instructor</exception>
    /// <exception cref="EntityServiceException">Thrown when instructor update fails</exception>
    public async Task<Instructor> UpdateInstructorAsync(Guid id, UpdateInstructor updateInstructor, ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Updating instructor with ID: {Id}", id);

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor update failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor update failed: Instructor with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor update failed: User not authorized to update instructor with ID {Id}", id);
                throw new EntityUnauthorizedException("Instructor", $"Update instructor with ID {id}", userId, "You are not authorized to update this instructor record");
            }

            if (!string.IsNullOrEmpty(updateInstructor.Firstname))
            {
                existingInstructor.Firstname = updateInstructor.Firstname;
            }

            if (!string.IsNullOrEmpty(updateInstructor.Lastname))
            {
                existingInstructor.Lastname = updateInstructor.Lastname;
            }

            if (updateInstructor.Department != null)
            {
                existingInstructor.Department = string.IsNullOrWhiteSpace(updateInstructor.Department)
                    ? null
                    : updateInstructor.Department.Trim();
            }

            existingInstructor.UpdatedAt = DateTime.UtcNow;

            var updatedInstructor = await _instructorRepository.UpdateInstructorAsync(existingInstructor).ConfigureAwait(false);
            await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated instructor with ID: {Id}", id);
            return updatedInstructor;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            // Re-throw EntityUnauthorizedException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating instructor with ID: {Id}", id);
            throw new EntityServiceException("Instructor", $"UpdateInstructor: {id}", "An error occurred while updating the instructor", ex);
        }
    }
    #endregion

    #region SoftDeleteInstructorAsync
    /// <summary>
    /// Soft deletes an instructor record
    /// </summary>
    /// <param name="id">The ID of the instructor to delete</param>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to delete the instructor</exception>
    /// <exception cref="EntityServiceException">Thrown when instructor deletion fails</exception>
    public async Task SoftDeleteInstructorAsync(Guid id, ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Soft deleting instructor with ID: {Id}", id);

            if (id == Guid.Empty)
            {
                _logger.LogWarning("Instructor soft delete failed: Invalid instructor ID {Id}", id);
                throw new ValidationException("Invalid instructor ID");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor soft delete failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor soft delete failed: Instructor with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor soft delete failed: User not authorized to delete instructor with ID {Id}", id);
                throw new EntityUnauthorizedException("Instructor", $"Soft delete instructor with ID {id}", userId, "You are not authorized to delete this instructor record");
            }

            var result = await _instructorRepository.SoftDeleteInstructorAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Instructor soft delete failed: Failed to soft delete instructor with ID {Id}", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully soft deleted instructor with ID: {Id}", id);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            // Re-throw EntityUnauthorizedException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while soft deleting instructor with ID: {Id}", id);
            throw new EntityServiceException("Instructor", $"SoftDeleteInstructor: {id}", "An error occurred while soft deleting the instructor", ex);
        }
    }
    #endregion

    #region HardDeleteInstructorAsync
    /// <summary>
    /// Hard deletes an instructor record
    /// </summary>
    /// <param name="id">The ID of the instructor to delete</param>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when instructor not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during deletion</exception>
    public async Task HardDeleteInstructorAsync(Guid id, ClaimsPrincipal userPrincipal)
    {
        _logger.LogInformation("Hard deleting instructor with ID: {Id}", id);

        try
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Instructor hard delete failed: Invalid instructor ID {Id}", id);
                throw new ValidationException("Invalid instructor ID");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor hard delete failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor hard delete failed: Instructor with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor hard delete failed: User not authorized to permanently delete instructor with ID {Id}", id);
                throw new EntityUnauthorizedException("Instructor", $"Hard delete instructor with ID {id}", userId, "You are not authorized to permanently delete this instructor record.");
            }

            var result = await _instructorRepository.HardDeleteInstructorAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Instructor hard delete failed: Failed to hard delete instructor with ID {Id}", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully hard deleted instructor with ID: {Id}", id);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while hard deleting instructor with ID: {Id}", id);
            throw ExceptionHandlingHelper.CreateServiceException("Instructor", $"HardDeleteInstructor: {id}", ex);
        }
    }
    #endregion

    #region RestoreInstructorAsync
    /// <summary>
    /// Restores a soft deleted instructor record
    /// </summary>
    /// <param name="id">The ID of the instructor to restore</param>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityNotFoundException{TKey}">Thrown when instructor not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during restore</exception>
    public async Task RestoreInstructorAsync(Guid id, ClaimsPrincipal userPrincipal)
    {
        _logger.LogInformation("Restoring instructor with ID: {Id}", id);

        try
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Instructor restore failed: Invalid instructor ID {Id}", id);
                throw new ValidationException("Invalid instructor ID");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor restore failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdIgnoreDeleteStatus(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor restore failed: Instructor with ID {Id} not found", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor restore failed: User not authorized to restore instructor with ID {Id}", id);
                throw new EntityUnauthorizedException("Instructor", $"Restore instructor with ID {id}", userId, "You are not authorized to restore this instructor record.");
            }

            // Check if instructor is actually deleted before restoring
            if (existingInstructor.DeletedAt == null)
            {
                _logger.LogWarning("Instructor restore failed: Instructor with ID {Id} is not deleted", id);
                throw new ValidationException("Instructor is not deleted");
            }

            var result = await _instructorRepository.RestoreInstructorAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Instructor restore failed: Failed to restore instructor with ID {Id}", id);
                throw new EntityNotFoundException<Guid>("Instructor", id);
            }

            await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully restored instructor with ID: {Id}", id);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while restoring instructor with ID: {Id}", id);
            throw ExceptionHandlingHelper.CreateServiceException("Instructor", $"RestoreInstructor: {id}", ex);
        }
    }
    #endregion
}
