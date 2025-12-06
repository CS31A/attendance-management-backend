using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface ISubjectService
{
    Task<IEnumerable<Subject>> GetAllSubjectsAsync();
    Task<Subject?> GetSubjectByIdAsync(int id);
    Task<Subject> CreateSubjectAsync(CreateSubject createSubject);
    Task<Subject> UpdateSubjectAsync(int id, UpdateSubject updateSubject);
    Task DeleteSubjectAsync(int id);
}