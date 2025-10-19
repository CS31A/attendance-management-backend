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
        Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(int instructorId);
        Task<(Schedules?, string?)> CreateScheduleAsync(CreateSchedule createSchedule);
        Task<(Schedules?, string?)> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule);
        Task<string?> DeleteScheduleAsync(int id, ClaimsPrincipal user);
    }
}