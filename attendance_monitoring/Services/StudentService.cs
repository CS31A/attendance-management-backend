using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly UserContextService _userContextService;

        public StudentService(IStudentRepository studentRepository, UserContextService userContextService)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync(PaginationQuery paginationQuery)
        {
            return await _studentRepository.GetAllStudentsAsync(paginationQuery);
        }

        public async Task<Student> GetStudentByIdAsync(int id)
        {
            return await _studentRepository.GetStudentByIdAsync(id);
        }

        public async Task<(Student, string)> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal userPrincipal)
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

        public async Task<(Student, string)> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal userPrincipal)
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
