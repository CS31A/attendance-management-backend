using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IStudentService
{
    Task<IList<Student>> GetAllStudentsAsync();
    Task<IList<StudentListDto>> GetAllNonDeletedStudentsAsync();
    Task<Student> GetStudentByIdAsync(Guid id);
    Task<Student> GetStudentByUuidAsync(Guid id);
    Task<Student> CreateStudentAsync(CreateStudent createStudent, ClaimsPrincipal user);
    Task<Student> UpdateStudentAsync(Guid id, UpdateStudent updateStudent, ClaimsPrincipal user);
    Task SoftDeleteStudentAsync(Guid id, ClaimsPrincipal user);
    Task<string?> HardDeleteStudentAsync(Guid id, ClaimsPrincipal user);
    Task<string?> RestoreStudentAsync(Guid id, ClaimsPrincipal user);
    Task<IEnumerable<StudentSubjectResponseDto>> GetStudentSubjectsAsync(ClaimsPrincipal user);
    Task<IEnumerable<StudentListDto>> SearchStudentsByNameAsync(string searchTerm, int pageNumber, int pageSize);
    Task<IEnumerable<StudentListDto>> SearchStudentsByEmailAsync(string searchTerm, int pageNumber, int pageSize);
}
