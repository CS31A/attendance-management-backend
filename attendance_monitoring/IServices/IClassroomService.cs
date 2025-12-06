using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IClassroomService
{
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync();
    Task<Classroom?> GetClassroomByIdAsync(int id);
    Task<Classroom> CreateClassroomAsync(CreateClassroom createClassroom);
    Task<Classroom> UpdateClassroomAsync(int id, UpdateClassroom updateClassroom);
    Task DeleteClassroomAsync(int id);
}
