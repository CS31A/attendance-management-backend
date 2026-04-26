using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IClassroomService
{
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync();
    Task<Classroom?> GetClassroomByIdAsync(Guid id);
    Task<Classroom?> GetClassroomByUuidAsync(Guid id);
    Task<Classroom> CreateClassroomAsync(CreateClassroom createClassroom);
    Task<Classroom> UpdateClassroomAsync(Guid id, UpdateClassroom updateClassroom);
    Task<Classroom> UpdateClassroomByUuidAsync(Guid id, UpdateClassroom updateClassroom);
    Task DeleteClassroomAsync(Guid id);
    Task DeleteClassroomByUuidAsync(Guid id);
    Task<bool> HasSchedulesInClassroomAsync(Guid id);
    Task<bool> HasSessionsInClassroomAsync(Guid id);
}
