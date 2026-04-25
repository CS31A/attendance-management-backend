using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleResponseDto>> GetAllSchedulesAsync();
        Task<ScheduleResponseDto> GetScheduleByIdAsync(int id);
        Task<ScheduleResponseDto> GetScheduleByUuidAsync(Guid uuid);
        Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorIdAsync(int instructorId);
        Task<IEnumerable<ScheduleResponseDto>> GetSchedulesBySectionIdAsync(int sectionId);
        Task<Schedules> CreateScheduleAsync(CreateSchedule createSchedule);
        Task<Schedules> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule);
        Task<Schedules> UpdateScheduleByUuidAsync(Guid uuid, UpdateSchedule updateSchedule);
        Task DeleteScheduleAsync(int id, ClaimsPrincipal user);
        Task DeleteScheduleByUuidAsync(Guid uuid, ClaimsPrincipal user);
        Task<bool> HasSessionsInScheduleAsync(int id);
        Task<IEnumerable<ScheduleResponseDto>> GetMySchedulesAsync();
    }
}
