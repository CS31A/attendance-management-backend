using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentService(IStudentRepository studentRepository, UserManager<IdentityUser> userManager)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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

            var userId = await GetUserIdAsync(userPrincipal);
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

            var userId = await GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingStudent = await _studentRepository.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return (null, "Student not found");
            }

            var userRole = userPrincipal.FindFirst(ClaimTypes.Role)?.Value;
            var isAuthorized = existingStudent.UserId == userId ||
                               (userRole != null && (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                                    userRole.Equals("Teacher", StringComparison.OrdinalIgnoreCase)));

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

            var updatedStudent = await _studentRepository.UpdateStudentAsync(existingStudent);
            await _studentRepository.SaveChangesAsync();

            return (updatedStudent, null);
        }

        private async Task<string> GetUserIdAsync(ClaimsPrincipal userPrincipal)
        {
            // Null check for defense in depth
            if (userPrincipal == null)
            {
                return null;
            }

            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId) && userId == userPrincipal.FindFirst(ClaimTypes.Name)?.Value)
            {
                var username = userId;
                // Null check for _userManager
                if (_userManager != null)
                {
                    var user = await _userManager.FindByNameAsync(username);
                    if (user != null)
                    {
                        return user.Id;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            userId = userPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
            return userId;
        }
    }
}
