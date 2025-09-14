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

        /// <summary>
        /// Initializes a new instance of the StudentService class
        /// </summary>
        /// <param name="studentRepository">Repository for student data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        public StudentService(IStudentRepository studentRepository, UserContextService userContextService)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
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
            // Additional validation for defense in depth
            if (createStudent == null)
            {
                return (null, "Create student data is required");
            }

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
            // Additional validation for defense in depth
            if (updateStudent == null)
            {
                return (null, "Update student data is required");
            }

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

    }
}
