using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IServices;

public interface IStudentService
{
    Task<IEnumerable<Student>> GetAllStudentsAsync();
    Task<IEnumerable<Student>> GetAllNonDeletedStudentsAsync();
    Task<Student?> GetStudentByIdAsync(int id);
    Task<(Student?, string?)> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal user);
    Task<(Student?, string?)> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal user);
    Task<string?> SoftDeleteStudentAsync(int id, ClaimsPrincipal user);
    Task<string?> HardDeleteStudentAsync(int id, ClaimsPrincipal user);
    Task<string?> RestoreStudentAsync(int id, ClaimsPrincipal user);
}
