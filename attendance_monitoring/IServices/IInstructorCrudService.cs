using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IInstructorCrudService
{
    Task<IList<Instructor>> GetAllInstructorsAsync();
    Task<Instructor> GetInstructorByIdAsync(Guid id);
    Task<Instructor> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<Instructor> UpdateInstructorAsync(Guid id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
    Task SoftDeleteInstructorAsync(Guid id, ClaimsPrincipal user);
    Task HardDeleteInstructorAsync(Guid id, ClaimsPrincipal user);
    Task RestoreInstructorAsync(Guid id, ClaimsPrincipal user);
}
