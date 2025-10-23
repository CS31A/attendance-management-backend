using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IInstructorService
{
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync();
    Task<Instructor> GetInstructorByIdAsync(int id);
    Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(int instructorId);
    Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal user);
    Task<(Instructor?, string?)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<Instructor> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
    Task SoftDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task<string?> HardDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task<string?> RestoreInstructorAsync(int id, ClaimsPrincipal user);
}
