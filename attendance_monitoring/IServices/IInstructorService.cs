using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IInstructorService
{
    Task<IList<Instructor>> GetAllInstructorsAsync();
    Task<Instructor> GetInstructorByIdAsync(Guid id);
    Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(Guid instructorId);
    Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal user);
    Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal user);
    Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal user);
    Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal user);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal user, Guid sectionId);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal user, Guid sectionUuid);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal user, Guid studentId);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal user, Guid studentUuid);
    Task<Instructor> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<Instructor> UpdateInstructorAsync(Guid id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
    Task SoftDeleteInstructorAsync(Guid id, ClaimsPrincipal user);
    Task HardDeleteInstructorAsync(Guid id, ClaimsPrincipal user);
    Task RestoreInstructorAsync(Guid id, ClaimsPrincipal user);
}
