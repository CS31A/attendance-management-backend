using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface ISubjectService
{
    Task<IEnumerable<Subject>> GetAllSubjectsAsync();
    Task<Subject?> GetSubjectByIdAsync(Guid id);
    Task<Subject?> GetSubjectByUuidAsync(Guid id);
    Task<Subject> CreateSubjectAsync(CreateSubject createSubject);
    Task<Subject> UpdateSubjectAsync(Guid id, UpdateSubject updateSubject);
    Task<Subject> UpdateSubjectByUuidAsync(Guid id, UpdateSubject updateSubject);
    Task DeleteSubjectAsync(Guid id);
    Task DeleteSubjectByUuidAsync(Guid id);
    Task<bool> HasSchedulesInSubjectAsync(Guid id);
    Task<bool> HasEnrollmentsInSubjectAsync(Guid id);
}
