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
    /// Service class for managing student-related operations
    /// </summary>
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly UserContextService _userContextService;
        private readonly ISectionRepository _sectionRepository;
        private readonly ILogger<StudentService> _logger;

        /// <summary>
        /// Initializes a new instance of the StudentService class
        /// </summary>
        /// <param name="studentRepository">Repository for student data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        /// <param name="sectionRepository">Repository for section data operations</param>
        /// <param name="logger">Logger for logging operations</param>
        public StudentService(IStudentRepository studentRepository, UserContextService userContextService, ISectionRepository sectionRepository, ILogger<StudentService> logger)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Get Operations
        public async Task<IList<Student>> GetAllStudentsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all students");

                var students = await _studentRepository.GetAllStudentsAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully retrieved {Count} students", students.Count);
                return students;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving students");
                throw new EntityServiceException("Student", "GetAllStudents", "An error occurred while retrieving students", ex);
            }
        }

        /// <summary>
        /// Retrieves all non-deleted students
        /// </summary>
        /// <returns>A collection of non-deleted students</returns>
        public async Task<IList<Student>> GetAllNonDeletedStudentsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all non-deleted students");

                var students = await _studentRepository.GetAllNonDeletedStudentsAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully retrieved {Count} non-deleted students", students.Count);
                return students;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving non-deleted students");
                throw new EntityServiceException("Student", "GetAllNonDeletedStudents", "An error occurred while retrieving non-deleted students", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific student by ID
        /// </summary>
        /// <param name="id">The ID of the student to retrieve</param>
        /// <returns>The student with the specified ID</returns>
        /// <exception cref="EntityNotFoundException{int}">Thrown when the student is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<Student> GetStudentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving student by ID: {Id}", id);
                var student = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
                if (student == null)
                {
                    _logger.LogWarning("Student with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Student", id);
                }

                _logger.LogInformation("Successfully retrieved student with ID: {Id}", id);
                return student;
            }
            catch (EntityNotFoundException<int>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving student with ID: {Id}", id);
                throw new EntityServiceException("Student", $"GetStudentById: {id}", "An error occurred while retrieving the student", ex);
            }
        }

        #endregion

        #region Create Operations
        /// <summary>
        /// Creates a new student record
        /// </summary>
        /// <param name="createStudent">The student data to create</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>The created student</returns>
        /// <exception cref="EntityAlreadyExistsException{T}">Thrown when a student record already exists for the user</exception>
        /// <exception cref="EntityServiceException">Thrown when student creation fails</exception>
        public async Task<Student> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Creating new student with name: {FirstName} {LastName}", 
                    createStudent.Firstname, createStudent.Lastname);

                // Validate section ID
                if (createStudent.SectionId <= 0)
                {
                    _logger.LogWarning("Student creation failed: Invalid section ID");
                    throw new EntityServiceException("Student", "CreateStudent", "Invalid section ID");
                }

                // Validate that the SectionId exists
                var section = await _sectionRepository.GetSectionByIdAsync(createStudent.SectionId).ConfigureAwait(false);
                if (section == null)
                {
                    _logger.LogWarning("Student creation failed: The specified section does not exist");
                    throw new EntityServiceException("Student", "CreateStudent", "The specified section does not exist");
                }

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Student creation failed: User ID not found in token");
                    throw new EntityServiceException("Student", "CreateStudent", "User ID not found in token");
                }

                var existingStudent = await _studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
                if (existingStudent != null)
                {
                    _logger.LogWarning("Student creation failed: A student record already exists for this user");
                    throw new EntityAlreadyExistsException<string>("Student", "UserId", userId);
                }

                var student = new Student
                {
                    Firstname = createStudent.Firstname,
                    Lastname = createStudent.Lastname,
                    Email = createStudent.Email,
                    IsRegular = createStudent.IsRegular,
                    UserId = userId,
                    SectionId = createStudent.SectionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdStudent = await _studentRepository.CreateStudent(student).ConfigureAwait(false);
                await _studentRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully created student with ID: {Id} and name: {FirstName} {LastName}", 
                    createdStudent.Id, createdStudent.Firstname, createdStudent.Lastname);
                return createdStudent;
            }
            catch (EntityAlreadyExistsException<string>)
            {
                // Re-throw EntityAlreadyExistsException as-is
                throw;
            }
            catch (EntityServiceException)
            {
                // Re-throw EntityServiceException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating student with name: {FirstName} {LastName}", 
                    createStudent.Firstname, createStudent.Lastname);
                throw new EntityServiceException("Student", "CreateStudent", "An error occurred while creating the student", ex);
            }
        }

        #endregion

        #region Update Operations
        /// <summary>
        /// Updates an existing student record
        /// </summary>
        /// <param name="id">The ID of the student to update</param>
        /// <param name="updateStudent">The updated student data</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>The updated student</returns>
        /// <exception cref="EntityNotFoundException{int}">Thrown when the student is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to update the student</exception>
        /// <exception cref="EntityServiceException">Thrown when student update fails</exception>
        public async Task<Student> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Updating student with ID: {Id}", id);
                
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Student update failed: User ID not found in token");
                    throw new EntityServiceException("Student", $"UpdateStudent: {id}", "User ID not found in token");
                }

                var existingStudent = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
                if (existingStudent == null)
                {
                    _logger.LogWarning("Student update failed: Student with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Student", id);
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher").ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Student update failed: User not authorized to update student with ID {Id}", id);
                    throw new EntityUnauthorizedException("Student", $"Update student with ID {id}", "You are not authorized to update this student record");
                }

                if (!string.IsNullOrEmpty(updateStudent.Firstname))
                {
                    existingStudent.Firstname = updateStudent.Firstname;
                }

                if (!string.IsNullOrEmpty(updateStudent.Lastname))
                {
                    existingStudent.Lastname = updateStudent.Lastname;
                }

                if (!string.IsNullOrEmpty(updateStudent.Email))
                {
                    existingStudent.Email = updateStudent.Email;
                }

                if (updateStudent.IsRegular.HasValue)
                {
                    existingStudent.IsRegular = updateStudent.IsRegular.Value;
                }

                existingStudent.UpdatedAt = DateTime.UtcNow;

                var updatedStudent = await _studentRepository.UpdateStudentAsync(existingStudent).ConfigureAwait(false);
                await _studentRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully updated student with ID: {Id}", id);
                return updatedStudent;
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
                _logger.LogError(ex, "Error occurred while updating student with ID: {Id}", id);
                throw new EntityServiceException("Student", $"UpdateStudent: {id}", "An error occurred while updating the student", ex);
            }
        }

        #endregion

        #region Delete Operations
        /// <summary>
        /// Soft deletes a student record
        /// </summary>
        /// <param name="id">The ID of the student to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <exception cref="EntityNotFoundException{int}">Thrown when the student is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the user is not authorized to delete the student</exception>
        /// <exception cref="EntityServiceException">Thrown when student deletion fails</exception>
        public async Task SoftDeleteStudentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Soft deleting student with ID: {Id}", id);
                
                if (id <= 0)
                {
                    _logger.LogWarning("Student soft delete failed: Invalid student ID {Id}", id);
                    throw new EntityServiceException("Student", $"SoftDeleteStudent: {id}", "Invalid student ID");
                }

                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Student soft delete failed: User ID not found in token");
                    throw new EntityServiceException("Student", $"SoftDeleteStudent: {id}", "User ID not found in token");
                }

                var existingStudent = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
                if (existingStudent == null)
                {
                    _logger.LogWarning("Student soft delete failed: Student with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Student", id);
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher").ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Student soft delete failed: User not authorized to delete student with ID {Id}", id);
                    throw new EntityUnauthorizedException("Student", $"Soft delete student with ID {id}", "You are not authorized to delete this student record");
                }

                var result = await _studentRepository.SoftDeleteStudentAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    _logger.LogError("Student soft delete failed: Failed to soft delete student with ID {Id}", id);
                    throw new EntityServiceException("Student", $"SoftDeleteStudent: {id}", "Failed to soft delete student");
                }
                
                await _studentRepository.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully soft deleted student with ID: {Id}", id);
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
                _logger.LogError(ex, "Error occurred while soft deleting student with ID: {Id}", id);
                throw new EntityServiceException("Student", $"SoftDeleteStudent: {id}", "An error occurred while soft deleting the student", ex);
            }
        }

        /// <summary>
        /// Hard deletes a student record
        /// </summary>
        /// <param name="id">The ID of the student to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> HardDeleteStudentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Hard deleting student with ID: {Id}", id);
            
            if (id <= 0)
            {
                _logger.LogWarning("Student hard delete failed: Invalid student ID {Id}", id);
                return "Invalid student ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Student hard delete failed: User ID not found in token");
                return "User ID not found in token";
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
            if (existingStudent == null)
            {
                _logger.LogWarning("Student hard delete failed: Student with ID {Id} not found", id);
                return "Student not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Student hard delete failed: User not authorized to permanently delete student with ID {Id}", id);
                return "You are not authorized to permanently delete this student record.";
            }

            var result = await _studentRepository.HardDeleteStudentAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Student hard delete failed: Failed to hard delete student with ID {Id}", id);
                return "Failed to hard delete student";
            }

            try
            {
                await _studentRepository.SaveChangesAsync().ConfigureAwait(false);
                
                _logger.LogInformation("Successfully hard deleted student with ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while hard deleting student with ID: {Id}", id);
                return "An error occurred while hard deleting the student. Please try again later.";
            }
        }
        
        /// <summary>
        /// Restores a soft deleted student record
        /// </summary>
        /// <param name="id">The ID of the student to restore</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> RestoreStudentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Restoring student with ID: {Id}", id);
            
            if (id <= 0)
            {
                _logger.LogWarning("Student restore failed: Invalid student ID {Id}", id);
                return "Invalid student ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Student restore failed: User ID not found in token");
                return "User ID not found in token";
            }

            var existingStudent = await _studentRepository.GetStudentByIdIgnoreDeleteStatus(id).ConfigureAwait(false);
            if (existingStudent == null)
            {
                _logger.LogWarning("Student restore failed: Student with ID {Id} not found", id);
                return "Student not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Student restore failed: User not authorized to restore student with ID {Id}", id);
                return "You are not authorized to restore this student record.";
            }

            // Check if student is actually deleted before restoring
            if (existingStudent.DeletedAt == null)
            {
                _logger.LogWarning("Student restore failed: Student with ID {Id} is not deleted", id);
                return "Student is not deleted";
            }

            var result = await _studentRepository.RestoreStudentAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Student restore failed: Failed to restore student with ID {Id}", id);
                return "Failed to restore student";
            }
            
            try
            {
                await _studentRepository.SaveChangesAsync().ConfigureAwait(false);
                
                _logger.LogInformation("Successfully restored student with ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while restoring student with ID: {Id}", id);
                return "An error occurred while restoring the student. Please try again later.";
            }
        }

        #endregion

        #region Get Student Subjects
        /// <summary>
        /// Retrieves all subjects assigned to the authenticated student
        /// </summary>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A collection of subjects with schedule details for the student</returns>
        /// <exception cref="EntityNotFoundException{string}">Thrown when the student is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<StudentSubjectResponseDto>> GetStudentSubjectsAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                _logger.LogInformation("Retrieving subjects for authenticated student");
                
                var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get student subjects failed: User ID not found in token");
                    throw new EntityServiceException("Student", "GetStudentSubjects", "User ID not found in token");
                }

                // Verify that the user is a student
                var student = await _studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
                if (student == null)
                {
                    _logger.LogWarning("Get student subjects failed: No student record found for user {UserId}", userId);
                    throw new EntityNotFoundException<string>("Student", userId);
                }

                var subjectData = await _studentRepository.GetStudentSubjectsAsync(userId).ConfigureAwait(false);
                
                var response = subjectData.Select(data => new StudentSubjectResponseDto
                {
                    Subject = new SubjectResponseDto
                    {
                        Id = data.Subject.Id,
                        Name = data.Subject.Name,
                        Code = data.Subject.Code,
                        CreatedAt = data.Subject.CreatedAt,
                        UpdatedAt = data.Subject.UpdatedAt
                    },
                    Schedule = new StudentSubjectScheduleDto
                    {
                        Id = data.Schedule.Id,
                        TimeIn = data.Schedule.TimeIn,
                        TimeOut = data.Schedule.TimeOut,
                        DayOfWeek = data.Schedule.DayOfWeek
                    },
                    Instructor = new InstructorResponseDto
                    {
                        Id = data.Instructor.Id,
                        Firstname = data.Instructor.Firstname,
                        Lastname = data.Instructor.Lastname,
                        Email = data.Instructor.Email
                    },
                    Classroom = new ClassroomResponseDto
                    {
                        Id = data.Classroom.Id,
                        Name = data.Classroom.Name,
                        CreatedAt = data.Classroom.CreatedAt,
                        UpdatedAt = data.Classroom.UpdatedAt
                    }
                });

                _logger.LogInformation("Successfully retrieved subjects for student {UserId}", userId);
                return response;
            }
            catch (EntityNotFoundException<string>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (EntityServiceException)
            {
                // Re-throw EntityServiceException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving student subjects");
                throw new EntityServiceException("Student", "GetStudentSubjects", "An error occurred while retrieving student subjects", ex);
            }
        }
        #endregion
    }
}
