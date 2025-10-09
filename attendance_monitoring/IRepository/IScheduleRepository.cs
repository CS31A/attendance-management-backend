using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository
{
    public interface IScheduleRepository
    {
        Task<IEnumerable<Schedules>> GetAllSchedulesAsync();
        Task<Schedules?> GetScheduleByIdAsync(int id);
        Task<Schedules> AddScheduleAsync(Schedules schedule);
        Task<Schedules?> UpdateScheduleAsync(Schedules schedule);
        Task<bool> DeleteScheduleAsync(int id);
        Task<int> SaveChangesAsync();
    }
}