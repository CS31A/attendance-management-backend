using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository
{
    public interface IScheduleRepository : ISaveableRepository
    {
        Task<IEnumerable<Schedules>> GetAllSchedulesAsync();
        Task<Schedules?> GetScheduleByIdAsync(int id);
        Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(int instructorId);
        Task<Schedules> AddScheduleAsync(Schedules schedule);
        Task<Schedules?> UpdateScheduleAsync(Schedules schedule);
        Task<bool> DeleteScheduleAsync(int id);
    }
}