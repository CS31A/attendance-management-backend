using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
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

        public async Task<IList<Student>> GetAllStudentsAsync()
        {
            _logger.LogInformation("Retrieving all students");

            var students = await _studentRepository.GetAllStudentsAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully retrieved {Count} students", students.Count);
            return students;
        }

        /// <summary>
        /// Retrieves all non-deleted students
        /// </summary>
        /// <returns>A collection of non-deleted students</returns>
        public async Task<IList<Student>> GetAllNonDeletedStudentsAsync()
        {
            _logger.LogInformation("Retrieving all non-deleted students");

            var students = await _studentRepository.GetAllNonDeletedStudentsAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully retrieved {Count} non-deleted students", students.Count);
            return students;
        }

        /// <summary>
        /// Retrieves a specific student by ID
        /// </summary>
        /// <param name="id">The ID of the student to retrieve</param>
        /// <returns>The student with the specified ID, or null if not found</returns>
        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving student by ID: {Id}", id);
            var student = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
            if (student == null)
            {
                _logger.LogWarning("Student with ID {Id} not found", id);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved student with ID: {Id}", id);
            }
            return student;
        }

        /// <summary>
        /// Creates a new student record
        /// </summary>
        /// <param name="createStudent">The student data to create</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the created student (if successful) and an error message (if any)</returns>
        public async Task<(Student?, string?)> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Creating new student with name: {FirstName} {LastName}", 
                createStudent.Firstname, createStudent.Lastname);

            // Validate section ID
            if (createStudent.SectionId <= 0)
            {
                _logger.LogWarning("Student creation failed: Invalid section ID");
                return (null, "Invalid section ID");
            }

            // Validate that the SectionId exists
            var section = await _sectionRepository.GetSectionByIdAsync(createStudent.SectionId).ConfigureAwait(false);
            if (section == null)
            {
                _logger.LogWarning("Student creation failed: The specified section does not exist");
                return (null, "The specified section does not exist");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Student creation failed: User ID not found in token");
                return (null, "User ID not found in token");
            }

            var existingStudent = await _studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
            if (existingStudent != null)
            {
                _logger.LogWarning("Student creation failed: A student record already exists for this user");
                return (null, "A student record already exists for this user");
            }

            var student = new Student
            {
                Firstname = createStudent.Firstname,
                Lastname = createStudent.Lastname,
                Email = createStudent.Email,
                UserId = userId,
                SectionId = createStudent.SectionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdStudent = await _studentRepository.CreateStudent(student).ConfigureAwait(false);
            await _studentRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created student with ID: {Id} and name: {FirstName} {LastName}", 
                createdStudent.Id, createdStudent.Firstname, createdStudent.Lastname);
            return (createdStudent, null);
        }

        /// <summary>
        /// Updates an existing student record
        /// </summary>
        /// <param name="id">The ID of the student to update</param>
        /// <param name="updateStudent">The updated student data</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the updated student (if successful) and an error message (if any)</returns>
        public async Task<(Student?, string?)> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Updating student with ID: {Id}", id);
            
            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Student update failed: User ID not found in token");
                return (null, "User ID not found in token");
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
            if (existingStudent == null)
            {
                _logger.LogWarning("Student update failed: Student with ID {Id} not found", id);
                return (null, "Student not found");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Student update failed: User not authorized to update student with ID {Id}", id);
                return (null, "You are not authorized to update this student record.");
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

            existingStudent.UpdatedAt = DateTime.UtcNow;

            var updatedStudent = await _studentRepository.UpdateStudentAsync(existingStudent).ConfigureAwait(false);
            await _studentRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully updated student with ID: {Id}", id);
            return (updatedStudent, null);
        }

        /// <summary>
        /// Soft deletes a student record
        /// </summary>
        /// <param name="id">The ID of the student to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> SoftDeleteStudentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("Soft deleting student with ID: {Id}", id);
            
            if (id <= 0)
            {
                _logger.LogWarning("Student soft delete failed: Invalid student ID {Id}", id);
                return "Invalid student ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Student soft delete failed: User ID not found in token");
                return "User ID not found in token";
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id).ConfigureAwait(false);
            if (existingStudent == null)
            {
                _logger.LogWarning("Student soft delete failed: Student with ID {Id} not found", id);
                return "Student not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("Student soft delete failed: User not authorized to delete student with ID {Id}", id);
                return "You are not authorized to delete this student record.";
            }

            var result = await _studentRepository.SoftDeleteStudentAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("Student soft delete failed: Failed to soft delete student with ID {Id}", id);
                return "Failed to soft delete student";
            }
            
            _logger.LogInformation("Successfully soft deleted student with ID: {Id}", id);
            return null;
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

            _logger.LogInformation("Successfully hard deleted student with ID: {Id}", id);
            return null;
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
            
            _logger.LogInformation("Successfully restored student with ID: {Id}", id);
            return null;
        }
    }
}
