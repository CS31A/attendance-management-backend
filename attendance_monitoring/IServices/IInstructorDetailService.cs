using System.Security.Claims;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IInstructorDetailService
{
    Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal user);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal user, Guid sectionId);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal user, Guid sectionUuid);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal user, Guid studentId);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal user, Guid studentUuid);
}
