using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IInstructorService
{
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync();
    Task<Instructor> GetInstructorByIdAsync(int id);
    Task<(Instructor?, string?)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<Instructor> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
    Task SoftDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task<string?> HardDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task<string?> RestoreInstructorAsync(int id, ClaimsPrincipal user);
}
