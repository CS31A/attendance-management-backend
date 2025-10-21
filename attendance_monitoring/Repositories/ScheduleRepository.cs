using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories
{
    public class ScheduleRepository(ApplicationDbContext context) : IScheduleRepository
    {
        #region Read Operations

        #region GetAllSchedulesAsync
        public async Task<IEnumerable<Schedules>> GetAllSchedulesAsync()
        {
            return await context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                .ToListAsync();
        }
        #endregion

        #region GetScheduleByIdAsync
        public async Task<Schedules?> GetScheduleByIdAsync(int id)
        {
            return await context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        #endregion

        #region GetSchedulesByInstructorIdAsync
        public async Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(int instructorId)
        {
            return await context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                .Where(s => s.InstructorId == instructorId)
                .ToListAsync();
        }
        #endregion

        #region GetSubjectsByInstructorIdAsync
        public async Task<IEnumerable<Subject>> GetSubjectsByInstructorIdAsync(int instructorId)
        {
            return await context.Schedules
                .Where(s => s.InstructorId == instructorId)
                .Select(s => s.Subject)
                .Distinct()
                .OrderBy(subject => subject.Name)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region Write Operations

        #region AddScheduleAsync
        public async Task<Schedules> AddScheduleAsync(Schedules schedule)
        {
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            return schedule;
        }
        #endregion

        #region UpdateScheduleAsync
        public async Task<Schedules?> UpdateScheduleAsync(Schedules schedule)
        {
            var existingSchedule = await context.Schedules.FindAsync(schedule.Id);
            if (existingSchedule == null)
            {
                return null;
            }

            context.Entry(existingSchedule).CurrentValues.SetValues(schedule);
            await context.SaveChangesAsync();
            return existingSchedule;
        }
        #endregion

        #region DeleteScheduleAsync
        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return false;
            }

            context.Schedules.Remove(schedule);
            return true;
        }
        #endregion

        #endregion

        #region Utility Operations

        #region SaveChangesAsync
        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
        #endregion

        #endregion
    }
}