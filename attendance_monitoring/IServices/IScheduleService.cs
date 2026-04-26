using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleResponseDto>> GetAllSchedulesAsync();
        Task<ScheduleResponseDto> GetScheduleByIdAsync(Guid id);
        Task<ScheduleResponseDto> GetScheduleByUuidAsync(Guid id);
        Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorIdAsync(Guid instructorId);
        Task<IEnumerable<ScheduleResponseDto>> GetSchedulesBySectionIdAsync(Guid sectionId);
        Task<Schedules> CreateScheduleAsync(CreateSchedule createSchedule);
        Task<Schedules> UpdateScheduleAsync(Guid id, UpdateSchedule updateSchedule);
        Task<Schedules> UpdateScheduleByUuidAsync(Guid id, UpdateSchedule updateSchedule);
        Task DeleteScheduleAsync(Guid id, ClaimsPrincipal user);
        Task DeleteScheduleByUuidAsync(Guid id, ClaimsPrincipal user);
        Task<bool> HasSessionsInScheduleAsync(Guid id);
        Task<IEnumerable<ScheduleResponseDto>> GetMySchedulesAsync();
    }
}
