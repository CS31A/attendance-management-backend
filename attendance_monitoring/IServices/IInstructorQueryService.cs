using System.Security.Claims;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IInstructorQueryService
{
    Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal user);
    Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(Guid instructorId);
    Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal user);
    Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal user);
}
