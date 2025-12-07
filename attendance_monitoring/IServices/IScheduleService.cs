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
        Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorIdAsync(int instructorId);
        Task<Schedules> CreateScheduleAsync(CreateSchedule createSchedule);
        Task<Schedules> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule);
        Task DeleteScheduleAsync(int id, ClaimsPrincipal user);
        Task<IEnumerable<ScheduleResponseDto>> GetMySchedulesAsync();
    }
}
