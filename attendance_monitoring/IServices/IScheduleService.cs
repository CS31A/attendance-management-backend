using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.IServices
{
    public interface IScheduleService
    {
        Task<IEnumerable<Schedules>> GetAllSchedulesAsync();
        Task<Schedules?> GetScheduleByIdAsync(int id);
        Task<(Schedules?, string?)> CreateScheduleAsync(CreateSchedule createSchedule);
        Task<(Schedules?, string?)> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule);
        Task<string?> SoftDeleteScheduleAsync(int id, ClaimsPrincipal user);
        Task<string?> HardDeleteScheduleAsync(int id, ClaimsPrincipal user);
        Task<string?> RestoreScheduleAsync(int id, ClaimsPrincipal user);
    }
}