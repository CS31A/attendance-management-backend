using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IClassroomService
{
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync();
    Task<Classroom?> GetClassroomByIdAsync(int id);
    Task<Classroom?> GetClassroomByUuidAsync(Guid uuid);
    Task<Classroom> CreateClassroomAsync(CreateClassroom createClassroom);
    Task<Classroom> UpdateClassroomAsync(int id, UpdateClassroom updateClassroom);
    Task<Classroom> UpdateClassroomByUuidAsync(Guid uuid, UpdateClassroom updateClassroom);
    Task DeleteClassroomAsync(int id);
    Task DeleteClassroomByUuidAsync(Guid uuid);
    Task<bool> HasSchedulesInClassroomAsync(int id);
    Task<bool> HasSessionsInClassroomAsync(int id);
}
