using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.InstructorServices;

namespace attendance_monitoring.Services;

/// <summary>
/// Public facade for instructor operations used by controllers and other callers.
/// Delegates work to focused instructor units for CRUD, queries, and detail operations.
/// </summary>
public class InstructorService : IInstructorService
{
    private readonly IInstructorCrudService _crudService;
    private readonly IInstructorQueryService _queryService;
    private readonly IInstructorDetailService _detailService;

    internal InstructorService(
        IInstructorCrudService crudService,
        IInstructorQueryService queryService,
        IInstructorDetailService detailService)
    {
        _crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _detailService = detailService ?? throw new ArgumentNullException(nameof(detailService));
    }

    // CRUD operations
    public Task<IList<Instructor>> GetAllInstructorsAsync() =>
        _crudService.GetAllInstructorsAsync();

    public Task<Instructor> GetInstructorByIdAsync(Guid id) =>
        _crudService.GetInstructorByIdAsync(id);

    public Task<Instructor> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user) =>
        _crudService.CreateInstructorAsync(createInstructor, user);

    public Task<Instructor> UpdateInstructorAsync(Guid id, UpdateInstructor updateInstructor, ClaimsPrincipal user) =>
        _crudService.UpdateInstructorAsync(id, updateInstructor, user);

    public Task SoftDeleteInstructorAsync(Guid id, ClaimsPrincipal user) =>
        _crudService.SoftDeleteInstructorAsync(id, user);

    public Task HardDeleteInstructorAsync(Guid id, ClaimsPrincipal user) =>
        _crudService.HardDeleteInstructorAsync(id, user);

    public Task RestoreInstructorAsync(Guid id, ClaimsPrincipal user) =>
        _crudService.RestoreInstructorAsync(id, user);

    // Query operations
    public Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal user) =>
        _queryService.GetInstructorProfileAsync(user);

    public Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(Guid instructorId) =>
        _queryService.GetSubjectsByInstructorIdAsync(instructorId);

    public Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal user) =>
        _queryService.GetSchedulesByInstructorAsync(user);

    public Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal user) =>
        _queryService.GetSectionsWithStudentsByInstructorAsync(user);

    // Detail operations
    public Task<List<InstructorSectionOverviewDto>> GetInstructorSectionsOverviewAsync(ClaimsPrincipal user) =>
        _detailService.GetInstructorSectionsOverviewAsync(user);

    public Task<InstructorSectionDetailDto> GetInstructorSectionDetailAsync(ClaimsPrincipal user, Guid sectionId) =>
        _detailService.GetInstructorSectionDetailAsync(user, sectionId);

    public Task<InstructorSectionDetailDto> GetInstructorSectionDetailByUuidAsync(ClaimsPrincipal user, Guid sectionUuid) =>
        _detailService.GetInstructorSectionDetailByUuidAsync(user, sectionUuid);

    public Task<InstructorStudentDetailDto> GetInstructorStudentDetailAsync(ClaimsPrincipal user, Guid studentId) =>
        _detailService.GetInstructorStudentDetailAsync(user, studentId);

    public Task<InstructorStudentDetailDto> GetInstructorStudentDetailByUuidAsync(ClaimsPrincipal user, Guid studentUuid) =>
        _detailService.GetInstructorStudentDetailByUuidAsync(user, studentUuid);
}
