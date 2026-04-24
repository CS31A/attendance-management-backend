using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IInstructorService
{
    Task<IList<Instructor>> GetAllInstructorsAsync();
    Task<Instructor> GetInstructorByIdAsync(int id);
    Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(int instructorId);
    Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal user);
    Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal user);
    Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal user);
    Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal user);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal user, int sectionId);
    Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal user, Guid sectionUuid);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal user, int studentId);
    Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal user, Guid studentUuid);
    Task<Instructor> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<Instructor> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
    Task SoftDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task HardDeleteInstructorAsync(int id, ClaimsPrincipal user);
    Task RestoreInstructorAsync(int id, ClaimsPrincipal user);
}
