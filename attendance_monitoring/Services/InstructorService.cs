using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
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
        private readonly ISectionRepository _sectionRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IFingerprintRepository _fingerprintRepository;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<InstructorService> _logger;

        /// <summary>
        /// Initializes a new instance of the InstructorService class
        /// </summary>
        /// <param name="instructorRepository">Repository for instructor data operations</param>
        /// <param name="sectionRepository">Repository for section data operations</param>
        /// <param name="studentRepository">Repository for student data operations</param>
        /// <param name="scheduleRepository">Repository for schedule data operations</param>
        /// <param name="fingerprintRepository">Repository for fingerprint data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        /// <param name="logger">Logger for logging operations</param>
        public InstructorService(IInstructorRepository instructorRepository, ISectionRepository sectionRepository, IStudentRepository studentRepository, IScheduleRepository scheduleRepository, IFingerprintRepository fingerprintRepository, IUserContextService userContextService, ILogger<InstructorService> logger)
        {
            _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
            _fingerprintRepository = fingerprintRepository ?? throw new ArgumentNullException(nameof(fingerprintRepository));
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
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
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
                    Id = s.Uuid,
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

        #region GetSchedulesByInstructorAsync
        /// <summary>
        /// Retrieves all schedules for the current authenticated instructor
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A collection of schedules for the instructor</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Retrieving schedules for authenticated instructor");

                // Extract user ID from JWT claims
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    throw new EntityNotFoundException<string>("User", userId ?? "null");
                }

                // Get instructor by user ID
                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
                }

                // Get schedules for instructor
                var schedules = await _scheduleRepository.GetSchedulesByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
                var scheduleDtos = schedules.Select(ScheduleServiceSupport.MapToResponseDto).ToList();

                _logger.LogInformation("Successfully retrieved {Count} schedules for instructor ID: {InstructorId}",
                    scheduleDtos.Count, instructor.Id);
                return scheduleDtos;
            }
            catch (EntityNotFoundException<string>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving schedules for authenticated instructor");
                throw new EntityServiceException("Instructor", "GetSchedulesByInstructor",
                    "An error occurred while retrieving instructor schedules", ex);
            }
        }
        #endregion

        #region GetInstructorProfileAsync
        /// <summary>
        /// Retrieves the instructor profile for the current authenticated user
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>The instructor profile if found, null otherwise</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Retrieving instructor profile for authenticated user");

                // Extract user ID from JWT claims
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    return null;
                }

                // Get instructor by user ID
                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    return null;
                }

                // Map to response DTO
                var profileDto = new InstructorProfileResponseDto
                {
                    Id = instructor.Id,
                    Firstname = instructor.Firstname,
                    Lastname = instructor.Lastname,
                    Department = instructor.Department,
                    Email = instructor.User?.Email,
                    CreatedAt = instructor.CreatedAt,
                    UpdatedAt = instructor.UpdatedAt
                };

                _logger.LogInformation("Successfully retrieved instructor profile for user ID: {UserId}, instructor ID: {InstructorId}",
                    userId, instructor.Id);
                return profileDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving instructor profile");
                throw new EntityServiceException("Instructor", "GetInstructorProfile",
                    "An error occurred while retrieving the instructor profile", ex);
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
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
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
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="EntityNotFoundException{TKey}">Thrown when instructor not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during deletion</exception>
        public async Task HardDeleteInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Hard deleting instructor with ID: {Id}", id);

            try
            {
                if (id <= 0)
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
                    throw new EntityNotFoundException<int>("Instructor", id);
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
                    throw new EntityServiceException("Instructor", $"HardDeleteInstructor: {id}", "Failed to hard delete instructor");
                }

                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully hard deleted instructor with ID: {Id}", id);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                throw;
            }
            catch (EntityServiceException)
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
        public async Task RestoreInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Restoring instructor with ID: {Id}", id);

            try
            {
                if (id <= 0)
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
                    throw new EntityNotFoundException<int>("Instructor", id);
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
                    throw new EntityServiceException("Instructor", $"RestoreInstructor: {id}", "Failed to restore instructor");
                }

                await _instructorRepository.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully restored instructor with ID: {Id}", id);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                throw;
            }
            catch (EntityServiceException)
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

        #region GetSectionsWithStudentsByInstructorAsync
        /// <summary>
        /// Retrieves all sections with students for the current authenticated instructor
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>Instructor sections with students response DTO</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Retrieving sections with students for authenticated instructor");

                // Extract user ID from JWT claims
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    throw new EntityNotFoundException<string>("User", userId ?? "null");
                }

                // Get instructor by user ID
                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
                }

                _logger.LogInformation("Getting sections with students for instructor ID: {InstructorId}", instructor.Id);

                // Get schedules with related data from repository
                var schedules = await _instructorRepository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
                var schedulesList = schedules.ToList();

                if (schedulesList.Count == 0)
                {
                    _logger.LogInformation("No schedules found for instructor ID: {InstructorId}", instructor.Id);
                    return new InstructorSectionsWithStudentsResponseDto
                    {
                        InstructorId = instructor.Uuid,
                        InstructorFirstname = instructor.Firstname ?? string.Empty,
                        InstructorLastname = instructor.Lastname ?? string.Empty,
                        Sections = new List<SectionWithStudentsDto>()
                    };
                }

                // Group schedules by section
                var sectionGroups = schedulesList
                    .GroupBy(s => s.SectionId)
                    .ToList();

                var sectionDtos = new List<SectionWithStudentsDto>();

                foreach (var sectionGroup in sectionGroups)
                {
                    var firstSchedule = sectionGroup.First();
                    var section = firstSchedule.Section;
                    var regularStudents = (await _instructorRepository.GetRegularStudentsBySectionIdAsync(section.Id).ConfigureAwait(false))
                        .Select(student => new StudentDto
                        {
                            StudentId = student.Uuid,
                            Firstname = student.Firstname,
                            Lastname = student.Lastname,
                            IsRegular = true,
                            EnrollmentType = EnrollmentTypeConstants.Regular
                        })
                        .ToList();

                    // Group schedules by subject within this section
                    var subjectSchedules = sectionGroup
                        .GroupBy(s => s.SubjectId)
                        .Select(subjectGroup =>
                        {
                            var schedule = subjectGroup.First();

                            // Regular students come from primary section membership.
                            // Irregular and retake students come from explicit additional enrollments.
                            var irregularStudents = section.StudentEnrollments
                                .Where(se => se.SubjectId == schedule.SubjectId 
                                    && !se.Student.IsDeleted
                                    && se.IsActive
                                    && se.Student.SectionId != section.Id)
                                .Select(se => new StudentDto
                                {
                                    StudentId = se.Student.Uuid,
                                    Firstname = se.Student.Firstname,
                                    Lastname = se.Student.Lastname,
                                    IsRegular = false,
                                    EnrollmentType = se.EnrollmentType
                                })
                                .ToList();

                            var enrolledStudents = regularStudents
                                .Concat(irregularStudents)
                                .GroupBy(s => s.StudentId)
                                .Select(g => g.First())
                                .OrderBy(s => s.Lastname)
                                .ThenBy(s => s.Firstname)
                                .ToList();

                            return new SubjectScheduleDto
                            {
                                SubjectId = schedule.Subject.Uuid,
                                SubjectName = schedule.Subject.Name,
                                SubjectCode = schedule.Subject.Code,
                                ScheduleId = schedule.Uuid,
                                DayOfWeek = schedule.DayOfWeek,
                                TimeIn = schedule.TimeIn,
                                TimeOut = schedule.TimeOut,
                                ClassroomId = schedule.Classroom.Uuid,
                                ClassroomName = schedule.Classroom.Name,
                                Students = enrolledStudents
                            };
                        })
                        .OrderBy(s => s.SubjectName)
                        .ToList();

                    sectionDtos.Add(new SectionWithStudentsDto
                    {
                        SectionId = section.Uuid,
                        SectionName = section.Name,
                        CourseId = GetRequiredCourse(section).Uuid,
                        CourseName = GetRequiredCourse(section).Name,
                        Subjects = subjectSchedules
                    });
                }

                var response = new InstructorSectionsWithStudentsResponseDto
                {
                    InstructorId = instructor.Uuid,
                    InstructorFirstname = instructor.Firstname ?? string.Empty,
                    InstructorLastname = instructor.Lastname ?? string.Empty,
                    Sections = sectionDtos.OrderBy(s => s.SectionName).ToList()
                };

                _logger.LogInformation("Successfully retrieved {SectionCount} sections with students for instructor ID: {InstructorId}",
                    sectionDtos.Count, instructor.Id);

                return response;
            }
            catch (EntityNotFoundException<string>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving sections with students for authenticated instructor");
                throw new EntityServiceException("Instructor", "GetSectionsWithStudentsByInstructor",
                    "An error occurred while retrieving sections with students", ex);
            }
        }
        #endregion

        #region GetInstructorSectionsOverviewAsync
        /// <summary>
        /// Retrieves a high-level overview of all sections handled by the authenticated instructor.
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A list of section overviews with class counts and student counts</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Retrieving sections overview for authenticated instructor");

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    throw new EntityNotFoundException<string>("User", userId ?? "null");
                }

                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
                }

                _logger.LogInformation("Getting handled sections for instructor ID: {InstructorId}", instructor.Id);

                var sections = await _instructorRepository.GetHandledSectionsByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
                var sectionList = sections.ToList();

                var overviewDtos = new List<InstructorSectionOverviewDto>();

                foreach (var section in sectionList)
                {
                    var schedules = await _instructorRepository.GetHandledClassesBySectionAndInstructorAsync(section.Id, instructor.Id).ConfigureAwait(false);
                    var schedulesList = schedules.ToList();
                    var handledSubjectIds = schedulesList
                        .Select(schedule => schedule.SubjectId)
                        .Distinct()
                        .ToHashSet();
                    var handledClassCount = handledSubjectIds.Count;

                    var uniqueStudentCount = 0;
                    if (handledClassCount > 0)
                    {
                        var regularStudents = await _instructorRepository.GetRegularStudentsBySectionIdAsync(section.Id).ConfigureAwait(false);
                        var regularStudentIds = regularStudents.Select(student => student.Id);
                        var sectionWithEnrollments = schedulesList.First().Section;
                        var irregularStudentIds = sectionWithEnrollments.StudentEnrollments
                            .Where(enrollment => handledSubjectIds.Contains(enrollment.SubjectId)
                                && enrollment.IsActive
                                && !enrollment.Student.IsDeleted
                                && enrollment.Student.SectionId != section.Id)
                            .Select(enrollment => enrollment.StudentId);

                        uniqueStudentCount = regularStudentIds
                            .Concat(irregularStudentIds)
                            .Distinct()
                            .Count();
                    }

                    overviewDtos.Add(new InstructorSectionOverviewDto
                    {
                        SectionId = section.Uuid,
                        SectionName = section.Name,
                        CourseId = GetRequiredCourse(section).Uuid,
                        CourseName = GetRequiredCourse(section).Name,
                        HandledClassCount = handledClassCount,
                        UniqueStudentCount = uniqueStudentCount
                    });
                }

                _logger.LogInformation("Successfully retrieved {SectionCount} section overviews for instructor ID: {InstructorId}",
                    overviewDtos.Count, instructor.Id);

                return overviewDtos.OrderBy(s => s.SectionName).ToList();
            }
            catch (EntityNotFoundException<string>)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving sections overview for authenticated instructor");
                throw new EntityServiceException("Instructor", "GetInstructorSectionsOverview",
                    "An error occurred while retrieving sections overview", ex);
            }
        }
        #endregion

        #region GetInstructorSectionDetailAsync
        /// <summary>
        /// Retrieves detailed information about a specific section handled by the authenticated instructor.
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <param name="sectionId">The section ID to retrieve details for</param>
        /// <returns>Detailed section information including handled classes and home section students</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the instructor is not authorized to view the section</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal userPrincipal, int sectionId)
        {
            try
            {
                _logger.LogInformation("Retrieving section detail for section ID: {SectionId}", sectionId);

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    throw new EntityNotFoundException<string>("User", userId ?? "null");
                }

                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
                }

                var sectionExists = await _sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
                if (sectionExists == null)
                {
                    _logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                    throw new EntityNotFoundException<int>("Section", sectionId);
                }

                var isHandlingSection = await _instructorRepository.IsInstructorHandlingSectionAsync(instructor.Id, sectionId).ConfigureAwait(false);
                if (!isHandlingSection)
                {
                    _logger.LogWarning("Instructor ID {InstructorId} is not authorized to view section ID: {SectionId}", instructor.Id, sectionId);
                    throw new EntityUnauthorizedException("Section", $"View section with ID {sectionId}", userId, "You are not authorized to view this section");
                }

                var schedules = await _instructorRepository.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructor.Id).ConfigureAwait(false);
                var schedulesList = schedules.ToList();

                var section = schedulesList.FirstOrDefault()?.Section;
                if (section == null)
                {
                    var handledSections = await _instructorRepository.GetHandledSectionsByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
                    section = handledSections.FirstOrDefault(s => s.Id == sectionId);
                }

                if (section == null)
                {
                    _logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                    throw new EntityNotFoundException<int>("Section", sectionId);
                }

                var handledClasses = new List<InstructorHandledClassDto>();
                var regularStudentEntities = (await _instructorRepository.GetRegularStudentsBySectionIdAsync(sectionId).ConfigureAwait(false)).ToList();
                var homeSectionStudentEntities = (await _instructorRepository.GetHomeSectionStudentsAsync(sectionId).ConfigureAwait(false)).ToList();
                var irregularStudentEntities = schedulesList
                    .SelectMany(schedule => section.StudentEnrollments
                        .Where(se => se.SubjectId == schedule.SubjectId
                            && !se.Student.IsDeleted
                            && se.IsActive
                            && se.Student.SectionId != sectionId)
                        .Select(se => se.Student))
                    .ToList();
                var fingerprintLookup = await BuildFingerprintLookupAsync(regularStudentEntities
                    .Concat(homeSectionStudentEntities)
                    .Concat(irregularStudentEntities))
                    .ConfigureAwait(false);

                var regularStudents = regularStudentEntities
                    .Select(student => CreateHandledClassStudentDto(
                        student,
                        true,
                        EnrollmentTypeConstants.Regular,
                        fingerprintLookup))
                    .ToList();

                foreach (var scheduleGroup in schedulesList.GroupBy(s => s.SubjectId))
                {
                    var schedule = scheduleGroup.First();

                    var irregularStudents = section.StudentEnrollments
                        .Where(se => se.SubjectId == schedule.SubjectId
                            && !se.Student.IsDeleted
                            && se.IsActive
                            && se.Student.SectionId != sectionId)
                        .Select(se => CreateHandledClassStudentDto(
                            se.Student,
                            false,
                            se.EnrollmentType,
                            fingerprintLookup))
                        .ToList();

                    var allStudents = regularStudents
                        .Concat(irregularStudents)
                        .GroupBy(s => s.StudentId)
                        .Select(g => g.First())
                        .OrderBy(s => s.Lastname)
                        .ThenBy(s => s.Firstname)
                        .ToList();

                    handledClasses.Add(new InstructorHandledClassDto
                    {
                        SubjectId = schedule.Subject.Uuid,
                        SubjectName = schedule.Subject.Name,
                        SubjectCode = schedule.Subject.Code,
                        ScheduleId = schedule.Uuid,
                        DayOfWeek = schedule.DayOfWeek,
                        TimeIn = schedule.TimeIn,
                        TimeOut = schedule.TimeOut,
                        ClassroomId = schedule.Classroom.Uuid,
                        ClassroomName = schedule.Classroom.Name,
                        StudentCount = allStudents.Count,
                        Students = allStudents
                    });
                }

                var homeSectionStudentDtos = homeSectionStudentEntities
                    .Select(student => CreateHomeSectionStudentDto(student, sectionId, fingerprintLookup))
                    .ToList();

                var detailDto = new InstructorSectionDetailDto
                {
                    SectionId = section.Uuid,
                    SectionName = section.Name,
                    CourseId = GetRequiredCourse(section).Uuid,
                    CourseName = GetRequiredCourse(section).Name,
                    HandledClassCount = handledClasses.Count,
                    HomeSectionStudentCount = homeSectionStudentDtos.Count,
                    HandledClasses = handledClasses.OrderBy(h => h.SubjectName).ToList(),
                    HomeSectionStudents = homeSectionStudentDtos.OrderBy(s => s.Lastname).ThenBy(s => s.Firstname).ToList()
                };

                _logger.LogInformation("Successfully retrieved section detail for section ID: {SectionId}, instructor ID: {InstructorId}",
                    sectionId, instructor.Id);

                return detailDto;
            }
            catch (EntityNotFoundException<string>)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving section detail for section ID: {SectionId}", sectionId);
                throw new EntityServiceException("Instructor", $"GetInstructorSectionDetail: {sectionId}",
                    "An error occurred while retrieving section detail", ex);
            }
        }
        #endregion

        public async Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal userPrincipal, Guid sectionUuid)
        {
            var section = await _sectionRepository.GetSectionByUuidAsync(sectionUuid).ConfigureAwait(false);
            if (section == null)
            {
                throw new EntityNotFoundException<Guid>("Section", sectionUuid);
            }

            return await GetInstructorSectionDetailAsync(userPrincipal, section.Id).ConfigureAwait(false);
        }

        #region GetInstructorStudentDetailAsync
        /// <summary>
        /// Retrieves detailed information about a specific student visible to the authenticated instructor.
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <param name="studentId">The student ID to retrieve details for</param>
        /// <returns>Detailed student information including enrollments and attendance summary</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the student is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the instructor is not authorized to view the student</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal userPrincipal, int studentId)
        {
            try
            {
                _logger.LogInformation("Retrieving student detail for student ID: {StudentId}", studentId);

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in JWT claims");
                    throw new EntityNotFoundException<string>("User", userId ?? "null");
                }

                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                    throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
                }

                var student = await _instructorRepository.GetStudentWithDetailsAsync(studentId).ConfigureAwait(false);
                if (student == null)
                {
                    _logger.LogWarning("Student with ID {StudentId} not found", studentId);
                    throw new EntityNotFoundException<int>("Student", studentId);
                }

                var isStudentVisible = await IsStudentVisibleToInstructorAsync(instructor.Id, student).ConfigureAwait(false);
                if (!isStudentVisible)
                {
                    _logger.LogWarning("Instructor ID {InstructorId} is not authorized to view student ID: {StudentId}", instructor.Id, studentId);
                    throw new EntityUnauthorizedException("Student", $"View student with ID {studentId}", userId, "You are not authorized to view this student");
                }

                // Build home-section enrollments from schedules taught by this instructor
                var instructorSchedules = await _instructorRepository
                    .GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructor.Id)
                    .ConfigureAwait(false);

                var homeSectionEnrollments = instructorSchedules
                    .GroupBy(schedule => schedule.SubjectId)
                    .Select(group => group.First())
                    .Select(schedule => new InstructorStudentEnrollmentDto
                    {
                        SubjectId = schedule.Subject.Uuid,
                        SubjectName = schedule.Subject.Name,
                        SubjectCode = schedule.Subject.Code,
                        SectionId = schedule.Section.Uuid,
                        SectionName = schedule.Section.Name,
                        EnrollmentType = EnrollmentTypeConstants.Regular
                    });

                var additionalEnrollments = student.AdditionalEnrollments
                    .Where(se => se.IsActive)
                    .Select(se => new InstructorStudentEnrollmentDto
                    {
                        SubjectId = se.Subject.Uuid,
                        SubjectName = se.Subject.Name,
                        SubjectCode = se.Subject.Code,
                        SectionId = se.Section.Uuid,
                        SectionName = se.Section.Name,
                        EnrollmentType = se.EnrollmentType
                    });

                var enrollments = homeSectionEnrollments
                    .Concat(additionalEnrollments)
                    .ToList();

                var attendanceRecords = await _instructorRepository.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructor.Id).ConfigureAwait(false);
                var attendanceList = attendanceRecords.ToList();

                var totalSessions = attendanceList.Count;
                var presentCount = attendanceList.Count(ar => ar.Status == "Present");
                var absentCount = attendanceList.Count(ar => ar.Status == "Absent");
                var lateCount = attendanceList.Count(ar => ar.Status == "Late");
                var attendanceRate = totalSessions > 0 ? (double)(presentCount + lateCount) / totalSessions * 100 : 0;

                var fingerprint = await _fingerprintRepository.GetFingerprintByStudentIdAsync(studentId).ConfigureAwait(false);
                InstructorStudentFingerprintDto? fingerprintDto = null;
                if (fingerprint != null && !fingerprint.IsDeleted)
                {
                    var devices = await _fingerprintRepository.GetDevicesAsync().ConfigureAwait(false);
                    var deviceLookup = BuildDeviceLookup(devices);
                    deviceLookup.TryGetValue(fingerprint.DeviceId, out var device);

                    fingerprintDto = new InstructorStudentFingerprintDto
                    {
                        Id = fingerprint.Uuid,
                        DeviceId = fingerprint.DeviceId,
                        DeviceName = device?.Name ?? fingerprint.DeviceId,
                        DeviceLocation = device?.Location ?? string.Empty,
                        EnrolledAt = fingerprint.CreatedAt
                    };
                }

                var detailDto = new InstructorStudentDetailDto
                {
                    StudentId = student.Uuid,
                    Firstname = student.Firstname,
                    Lastname = student.Lastname,
                    SectionId = student.Section?.Uuid,
                    SectionName = student.Section?.Name,
                    CourseId = student.Section?.Course?.Uuid,
                    CourseName = student.Section?.Course?.Name,
                    IsRegular = student.IsRegular,
                    EnrollmentType = student.IsRegular ? EnrollmentTypeConstants.Regular : EnrollmentTypeConstants.Irregular,
                    Enrollments = enrollments,
                    AttendanceSummary = new InstructorStudentAttendanceSummaryDto
                    {
                        TotalSessions = totalSessions,
                        PresentCount = presentCount,
                        AbsentCount = absentCount,
                        LateCount = lateCount,
                        AttendanceRate = Math.Round(attendanceRate, 2)
                    },
                    Fingerprint = fingerprintDto
                };

                _logger.LogInformation("Successfully retrieved student detail for student ID: {StudentId}, instructor ID: {InstructorId}",
                    studentId, instructor.Id);

                return detailDto;
            }
            catch (EntityNotFoundException<string>)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityUnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving student detail for student ID: {StudentId}", studentId);
                throw new EntityServiceException("Instructor", $"GetInstructorStudentDetail: {studentId}",
                    "An error occurred while retrieving student detail", ex);
            }
        }

        private async Task<bool> IsStudentVisibleToInstructorAsync(int instructorId, Student student)
        {
            var isHandlingStudentSection = await _instructorRepository.IsInstructorHandlingSectionAsync(instructorId, student.SectionId).ConfigureAwait(false);
            if (isHandlingStudentSection)
            {
                return true;
            }

            var instructorSchedules = await _scheduleRepository.GetSchedulesByInstructorIdAsync(instructorId).ConfigureAwait(false);
            var handledSectionSubjectPairs = instructorSchedules
                .Select(schedule => (schedule.SectionId, schedule.SubjectId))
                .ToHashSet();

            return student.AdditionalEnrollments.Any(studentEnrollment =>
                studentEnrollment.IsActive
                && handledSectionSubjectPairs.Contains((studentEnrollment.SectionId, studentEnrollment.SubjectId)));
        }
        #endregion

        public async Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal userPrincipal, Guid studentUuid)
        {
            var student = await _studentRepository.GetStudentByUuidAsync(studentUuid).ConfigureAwait(false);
            if (student == null)
            {
                throw new EntityNotFoundException<Guid>("Student", studentUuid);
            }

            return await GetInstructorStudentDetailAsync(userPrincipal, student.Id).ConfigureAwait(false);
        }

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

        private static Course GetRequiredCourse(Section section)
        {
            return section.Course
                ?? throw new InvalidOperationException($"Section {section.Id} is missing required course data.");
        }

        private async Task<Dictionary<int, StudentFingerprintDisplay>> BuildFingerprintLookupAsync(IEnumerable<Student> students)
        {
            var studentList = students
                .GroupBy(student => student.Id)
                .Select(group => group.First())
                .ToList();
            var studentUserIds = studentList
                .Select(student => student.UserId)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .ToHashSet(StringComparer.Ordinal);

            if (studentUserIds.Count == 0)
            {
                return new Dictionary<int, StudentFingerprintDisplay>();
            }

            var fingerprints = await _fingerprintRepository.GetActiveFingerprintsAsync().ConfigureAwait(false);
            var devices = await _fingerprintRepository.GetDevicesAsync().ConfigureAwait(false);
            var deviceLookup = BuildDeviceLookup(devices);
            var fingerprintByUserId = fingerprints
                .Where(fingerprint => studentUserIds.Contains(fingerprint.UserId))
                .GroupBy(fingerprint => fingerprint.UserId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            return studentList
                .Where(student => fingerprintByUserId.ContainsKey(student.UserId))
                .ToDictionary(
                    student => student.Id,
                    student =>
                    {
                        var fingerprint = fingerprintByUserId[student.UserId];
                        deviceLookup.TryGetValue(fingerprint.DeviceId, out var device);
                        return new StudentFingerprintDisplay(
                            fingerprint.DeviceId,
                            device?.Name);
                    });
        }

        private static Dictionary<string, FingerprintDevice> BuildDeviceLookup(IEnumerable<FingerprintDevice> devices)
        {
            return devices
                .Where(device => !string.IsNullOrWhiteSpace(device.DeviceIdentifier))
                .GroupBy(device => device.DeviceIdentifier, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static InstructorHandledClassStudentDto CreateHandledClassStudentDto(
            Student student,
            bool isRegular,
            string enrollmentType,
            IReadOnlyDictionary<int, StudentFingerprintDisplay> fingerprintLookup)
        {
            fingerprintLookup.TryGetValue(student.Id, out var fingerprint);

            return new InstructorHandledClassStudentDto
            {
                StudentId = student.Uuid,
                Firstname = student.Firstname,
                Lastname = student.Lastname,
                IsRegular = isRegular,
                EnrollmentType = enrollmentType,
                HasFingerprint = fingerprint != null,
                FingerprintDeviceId = fingerprint?.DeviceId,
                FingerprintDeviceName = fingerprint?.DeviceName
            };
        }

        private static InstructorHomeSectionStudentDto CreateHomeSectionStudentDto(
            Student student,
            int sectionId,
            IReadOnlyDictionary<int, StudentFingerprintDisplay> fingerprintLookup)
        {
            fingerprintLookup.TryGetValue(student.Id, out var fingerprint);

            return new InstructorHomeSectionStudentDto
            {
                StudentId = student.Uuid,
                Firstname = student.Firstname,
                Lastname = student.Lastname,
                IsRegular = student.SectionId == sectionId,
                EnrollmentType = student.SectionId == sectionId ? EnrollmentTypeConstants.Regular : EnrollmentTypeConstants.Irregular,
                HasFingerprint = fingerprint != null,
                FingerprintDeviceId = fingerprint?.DeviceId,
                FingerprintDeviceName = fingerprint?.DeviceName
            };
        }

        private sealed record StudentFingerprintDisplay(string DeviceId, string? DeviceName);

        #endregion
    }
}
