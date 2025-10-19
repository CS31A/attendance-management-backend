using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services
{
    /// <summary>
    /// Service class for managing instructor-related operations
    /// </summary>

    #region Constructor and Dependencies

    public class InstructorService : IInstructorService
    {
        private readonly IInstructorRepository _instructorRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly UserContextService _userContextService;
        private readonly ILogger<InstructorService> _logger;

        /// <summary>
        /// Initializes a new instance of the InstructorService class
        /// </summary>
        /// <param name="instructorRepository">Repository for instructor data operations</param>
        /// <param name="scheduleRepository">Repository for schedule data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        /// <param name="logger">Logger for logging operations</param>
        public InstructorService(IInstructorRepository instructorRepository, IScheduleRepository scheduleRepository, UserContextService userContextService, ILogger<InstructorService> logger)
        {
            _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
            _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region GetAllInstructorsAsync
        /// <summary>
        /// Retrieves all instructors
        /// </summary>
        /// <returns>A collection of instructors</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all instructors");
                var instructors = await _instructorRepository.GetAllInstructorsAsync().ConfigureAwait(false);
                var allInstructorsAsync = instructors.ToList();
                _logger.LogInformation("Successfully retrieved {Count} instructors", allInstructorsAsync.ToList().Count);
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
        /// <exception cref="EntityNotFoundException{int}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<Instructor> GetInstructorByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving instructor by ID: {Id}", id);
                var instructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("Instructor with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Instructor", id);
                }

                _logger.LogInformation("Successfully retrieved instructor with ID: {Id}", id);
                return instructor;
            }
            catch (EntityNotFoundException<int>)
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

        #region GetSubjectsByInstructorIdAsync
        /// <summary>
        /// Retrieves all subjects taught by a specific instructor
        /// </summary>
        /// <param name="instructorId">The ID of the instructor</param>
        /// <returns>A collection of subjects taught by the instructor</returns>
        /// <exception cref="EntityNotFoundException{int}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(int instructorId)
        {
            try
            {
                _logger.LogInformation("Retrieving subjects for instructor ID: {InstructorId}", instructorId);
                
                // Verify instructor exists
                var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("Instructor with ID {InstructorId} not found", instructorId);
                    throw new EntityNotFoundException<int>("Instructor", instructorId);
                }

                // Get subjects from schedules
                var subjects = await _scheduleRepository.GetSubjectsByInstructorIdAsync(instructorId).ConfigureAwait(false);
                var subjectDtos = subjects.Select(s => new SubjectResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} subjects for instructor ID: {InstructorId}", 
                    subjectDtos.Count, instructorId);
                return subjectDtos;
            }
            catch (EntityNotFoundException<int>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving subjects for instructor ID: {InstructorId}", instructorId);
                throw new EntityServiceException("Instructor", $"GetSubjectsByInstructorId: {instructorId}", 
                    "An error occurred while retrieving instructor subjects", ex);
            }
        }
        #endregion

        #region CreateInstructorAsync
        /// <summary>
        /// Creates a new instructor record
        /// </summary>
        /// <param name="createInstructor">The instructor data to create</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the created instructor (if successful) and an error message (if any)</returns>
        public async Task<(Instructor?, string?)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Creating new instructor with name: {FirstName} {LastName}", 
                createInstructor.Firstname, createInstructor.Lastname);

            // Validate basic user information
            if (string.IsNullOrWhiteSpace(createInstructor.Firstname) || 
                string.IsNullOrWhiteSpace(createInstructor.Lastname) ||
                string.IsNullOrWhiteSpace(createInstructor.Email))
            {
                _logger.LogWarning("Instructor creation failed: First name, last name, and email are required");
                return (null, "First name, last name, and email are required");
            }

            // Validate email format
            if (!IsValidEmail(createInstructor.Email))
            {
                _logger.LogWarning("Instructor creation failed: Invalid email format");
                return (null, "Invalid email format");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor creation failed: User ID not found in token");
                return (null, "User ID not found in token");
            }
            
            // Check if instructor already exists for this user
            var existingInstructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (existingInstructor != null)
            {
                _logger.LogWarning("Instructor creation failed: An instructor record already exists for this user");
                return (null, "An instructor record already exists for this user");
            }

            var instructor = new Instructor
            {
                Firstname = createInstructor.Firstname,
                Lastname = createInstructor.Lastname,
                Email = createInstructor.Email,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var createdInstructor = await _instructorRepository.CreateInstructorAsync(instructor).ConfigureAwait(false);
                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully created instructor with ID: {Id} and name: {FirstName} {LastName}", 
                    createdInstructor.Id, createdInstructor.Firstname, createdInstructor.Lastname);
                return (createdInstructor, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating instructor with name: {FirstName} {LastName}", 
                    createInstructor.Firstname, createInstructor.Lastname);
                return (null, "An error occurred while creating the instructor. Please try again later.");
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
        /// <exception cref="EntityNotFoundException{int}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to update the instructor</exception>
        /// <exception cref="EntityServiceException">Thrown when instructor update fails</exception>
        public async Task<Instructor> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Updating instructor with ID: {Id}", id);
                
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Instructor update failed: User ID not found in token");
                    throw new EntityServiceException("Instructor", $"UpdateInstructor: {id}", "User ID not found in token");
                }

                var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
                if (existingInstructor == null)
                {
                    _logger.LogWarning("Instructor update failed: Instructor with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Instructor", id);
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin", "Teacher").ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Instructor update failed: User not authorized to update instructor with ID {Id}", id);
                    throw new EntityUnauthorizedException("Instructor", $"Update instructor with ID {id}", "You are not authorized to update this instructor record");
                }

                if (!string.IsNullOrEmpty(updateInstructor.Firstname))
                {
                    existingInstructor.Firstname = updateInstructor.Firstname;
                }

                if (!string.IsNullOrEmpty(updateInstructor.Lastname))
                {
                    existingInstructor.Lastname = updateInstructor.Lastname;
                }

                if (!string.IsNullOrEmpty(updateInstructor.Email))
                {
                    existingInstructor.Email = updateInstructor.Email;
                }

                existingInstructor.UpdatedAt = DateTime.UtcNow;

                var updatedInstructor = await _instructorRepository.UpdateInstructorAsync(existingInstructor).ConfigureAwait(false);
                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully updated instructor with ID: {Id}", id);
                return updatedInstructor;
            }
            catch (EntityNotFoundException<int>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                // Re-throw EntityUnauthorizedException as-is
                throw;
            }
            catch (EntityServiceException)
            {
                // Re-throw EntityServiceException as-is
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
        /// <exception cref="EntityNotFoundException{int}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to delete the instructor</exception>
        /// <exception cref="EntityServiceException">Thrown when instructor deletion fails</exception>
        public async Task SoftDeleteInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Soft deleting instructor with ID: {Id}", id);
                
                if (id <= 0)
                {
                    _logger.LogWarning("Instructor soft delete failed: Invalid instructor ID {Id}", id);
                    throw new EntityServiceException("Instructor", $"SoftDeleteInstructor: {id}", "Invalid instructor ID");
                }

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Instructor soft delete failed: User ID not found in token");
                    throw new EntityServiceException("Instructor", $"SoftDeleteInstructor: {id}", "User ID not found in token");
                }

                var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
                if (existingInstructor == null)
                {
                    _logger.LogWarning("Instructor soft delete failed: Instructor with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Instructor", id);
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin", "Teacher").ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Instructor soft delete failed: User not authorized to delete instructor with ID {Id}", id);
                    throw new EntityUnauthorizedException("Instructor", $"Soft delete instructor with ID {id}", "You are not authorized to delete this instructor record");
                }

                var result = await _instructorRepository.SoftDeleteInstructorAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    _logger.LogError("Instructor soft delete failed: Failed to soft delete instructor with ID {Id}", id);
                    throw new EntityServiceException("Instructor", $"SoftDeleteInstructor: {id}", "Failed to soft delete instructor");
                }
                
                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully soft deleted instructor with ID: {Id}", id);
            }
            catch (EntityNotFoundException<int>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                // Re-throw EntityUnauthorizedException as-is
                throw;
            }
            catch (EntityServiceException)
            {
                // Re-throw EntityServiceException as-is
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
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> HardDeleteInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Hard deleting instructor with ID: {Id}", id);
            
            if (id <= 0)
            {
                _logger.LogWarning("Instructor hard delete failed: Invalid instructor ID {Id}", id);
                return "Invalid instructor ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor hard delete failed: User ID not found in token");
                return "User ID not found in token";
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor hard delete failed: Instructor with ID {Id} not found", id);
                return "Instructor not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor hard delete failed: User not authorized to permanently delete instructor with ID {Id}", id);
                return "You are not authorized to permanently delete this instructor record.";
            }

            var result = await _instructorRepository.HardDeleteInstructorAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Instructor hard delete failed: Failed to hard delete instructor with ID {Id}", id);
                return "Failed to hard delete instructor";
            }
            
            try
            {
                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
                
                _logger.LogInformation("Successfully hard deleted instructor with ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while hard deleting instructor with ID: {Id}", id);
                return "An error occurred while hard deleting the instructor. Please try again later.";
            }
        }
        #endregion
        
        #region RestoreInstructorAsync
        /// <summary>
        /// Restores a soft deleted instructor record
        /// </summary>
        /// <param name="id">The ID of the instructor to restore</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> RestoreInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Restoring instructor with ID: {Id}", id);
            
            if (id <= 0)
            {
                _logger.LogWarning("Instructor restore failed: Invalid instructor ID {Id}", id);
                return "Invalid instructor ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Instructor restore failed: User ID not found in token");
                return "User ID not found in token";
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdIgnoreDeleteStatus(id).ConfigureAwait(false);
            if (existingInstructor == null)
            {
                _logger.LogWarning("Instructor restore failed: Instructor with ID {Id} not found", id);
                return "Instructor not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin", "Teacher").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Instructor restore failed: User not authorized to restore instructor with ID {Id}", id);
                return "You are not authorized to restore this instructor record.";
            }

            // Check if instructor is actually deleted before restoring
            if (existingInstructor.DeletedAt == null)
            {
                _logger.LogWarning("Instructor restore failed: Instructor with ID {Id} is not deleted", id);
                return "Instructor is not deleted";
            }

            var result = await _instructorRepository.RestoreInstructorAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Instructor restore failed: Failed to restore instructor with ID {Id}", id);
                return "Failed to restore instructor";
            }
            
            try
            {
                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
                
                _logger.LogInformation("Successfully restored instructor with ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while restoring instructor with ID: {Id}", id);
                return "An error occurred while restoring the instructor. Please try again later.";
            }
        }
        #endregion

        #region Helper Methods

        #region IsValidEmail
        /// <summary>
        /// Validates email format
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if the email is valid, false otherwise</returns>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #endregion
    }
}
