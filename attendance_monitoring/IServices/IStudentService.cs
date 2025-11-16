using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IStudentService
{
    Task<IList<Student>> GetAllStudentsAsync();
    Task<IList<Student>> GetAllNonDeletedStudentsAsync();
    Task<Student> GetStudentByIdAsync(int id);
    Task<Student> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal user);
    Task<Student> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal user);
    Task SoftDeleteStudentAsync(int id, ClaimsPrincipal user);
    Task<string?> HardDeleteStudentAsync(int id, ClaimsPrincipal user);
    Task<string?> RestoreStudentAsync(int id, ClaimsPrincipal user);
    Task<IEnumerable<StudentSubjectResponseDto>> GetStudentSubjectsAsync(ClaimsPrincipal user);
    Task<IEnumerable<Student>> SearchStudentsByNameAsync(string searchTerm, int pageNumber, int pageSize);
    Task<IEnumerable<Student>> SearchStudentsByEmailAsync(string searchTerm, int pageNumber, int pageSize);
}
