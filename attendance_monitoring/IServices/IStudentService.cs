using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IServices;

public interface IStudentService
{
    Task<IEnumerable<Student>> GetAllStudentsAsync(PaginationQuery paginationQuery);
    Task<Student?> GetStudentByIdAsync(int id);
    Task<(Student?, string?)> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal user);
    Task<(Student?, string?)> UpdateStudentAsync(int id, UpdateStudent updateStudent, ClaimsPrincipal user);
}
