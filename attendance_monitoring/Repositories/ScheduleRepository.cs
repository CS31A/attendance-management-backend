using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Read Operations

        #region GetAllSchedulesAsync
        public async Task<IEnumerable<Schedules>> GetAllSchedulesAsync()
        {
            return await _context.Schedules.ToListAsync();
        }
        #endregion

        #region GetScheduleByIdAsync
        public async Task<Schedules?> GetScheduleByIdAsync(int id)
        {
            return await _context.Schedules.FindAsync(id);
        }
        #endregion

        #endregion

        #region Write Operations

        #region AddScheduleAsync
        public async Task<Schedules> AddScheduleAsync(Schedules schedule)
        {
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }
        #endregion

        #region UpdateScheduleAsync
        public async Task<Schedules?> UpdateScheduleAsync(Schedules schedule)
        {
            var existingSchedule = await _context.Schedules.FindAsync(schedule.Id);
            if (existingSchedule == null)
            {
                return null;
            }

            _context.Entry(existingSchedule).CurrentValues.SetValues(schedule);
            await _context.SaveChangesAsync();
            return existingSchedule;
        }
        #endregion

        #region DeleteScheduleAsync
        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return false;
            }

            _context.Schedules.Remove(schedule);
            return true;
        }
        #endregion

        #endregion

        #region Utility Operations

        #region SaveChangesAsync
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        #endregion

        #endregion
    }
}