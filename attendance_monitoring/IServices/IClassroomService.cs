using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface IClassroomService
{
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync();
    Task<Classroom?> GetClassroomByIdAsync(int id);
    Task<(Classroom?, string?)> CreateClassroomAsync(CreateClassroom createClassroom);
    Task<(Classroom?, string?)> UpdateClassroomAsync(int id, UpdateClassroom updateClassroom);
    Task<string?> DeleteClassroomAsync(int id);
}
