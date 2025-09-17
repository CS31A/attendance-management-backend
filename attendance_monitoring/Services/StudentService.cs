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

        /// <summary>
        /// Initializes a new instance of the StudentService class
        /// </summary>
        /// <param name="studentRepository">Repository for student data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        /// <param name="sectionRepository">Repository for section data operations</param>
        public StudentService(IStudentRepository studentRepository, UserContextService userContextService, ISectionRepository sectionRepository)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        }

        /// <summary>
        /// Retrieves all students with pagination support
        /// </summary>
        /// <param name="paginationQuery">Pagination parameters</param>
        /// <returns>A collection of students</returns>
        public async Task<IEnumerable<Student>> GetAllStudentsAsync(PaginationQuery paginationQuery)
        {
            return await _studentRepository.GetAllStudentsAsync(paginationQuery);
        }

        /// <summary>
        /// Retrieves a specific student by ID
        /// </summary>
        /// <param name="id">The ID of the student to retrieve</param>
        /// <returns>The student with the specified ID, or null if not found</returns>
        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _studentRepository.GetStudentByIdAsync(id);
        }

        /// <summary>
        /// Creates a new student record
        /// </summary>
        /// <param name="createStudent">The student data to create</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the created student (if successful) and an error message (if any)</returns>
        public async Task<(Student?, string?)> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal userPrincipal)
        {
            if (string.IsNullOrWhiteSpace(createStudent.Firstname))
            {
                return (null, "First name is required");
            }

            if (string.IsNullOrWhiteSpace(createStudent.Lastname))
            {
                return (null, "Last name is required");
            }

            if (string.IsNullOrWhiteSpace(createStudent.Email))
            {
                return (null, "Email is required");
            }

            if (createStudent.SectionId <= 0)
            {
                return (null, "Valid SectionId is required");
            }

            // Validate that the SectionId exists
            var section = await _sectionRepository.GetSectionByIdAsync(createStudent.SectionId);
            if (section == null)
            {
                return (null, "The specified section does not exist");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingStudent = await _studentRepository.GetStudentByUserIdAsync(userId);
            if (existingStudent != null)
            {
                return (null, "Student record already exists for this user");
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

            var createdStudent = await _studentRepository.CreateStudent(student);
            await _studentRepository.SaveChangesAsync();

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
            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return (null, "Student not found");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher");
            if (!isAuthorized)
            {
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

            var updatedStudent = await _studentRepository.UpdateStudentAsync(existingStudent);
            await _studentRepository.SaveChangesAsync();

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
            if (id <= 0)
            {
                return "Invalid student ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return "User ID not found in token";
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return "Student not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin", "Teacher");
            if (!isAuthorized)
            {
                return "You are not authorized to delete this student record.";
            }

            var result = await _studentRepository.SoftDeleteStudentAsync(id);
            return !result ? "Failed to soft delete student" : null;
        }

        /// <summary>
        /// Hard deletes a student record
        /// </summary>
        /// <param name="id">The ID of the student to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> HardDeleteStudentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            if (id <= 0)
            {
                return "Invalid student ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return "User ID not found in token";
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return "Student not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingStudent.UserId, "Admin");
            if (!isAuthorized)
            {
                return "You are not authorized to permanently delete this student record.";
            }

            var result = await _studentRepository.HardDeleteStudentAsync(id);
            if (!result)
            {
                return "Failed to hard delete student";
            }

            return null;
        }
    }
}
