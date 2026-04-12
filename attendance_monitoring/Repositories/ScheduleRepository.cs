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
        /// <summary>
        /// Retrieves all schedules with navigation properties.
        /// Performance: Uses split query to avoid cartesian explosion with multiple includes.
        /// </summary>
        public async Task<IEnumerable<Schedules>> GetAllSchedulesAsync()
        {
            return await context.Schedules
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .ToListAsync();
        }
        #endregion

        #region GetScheduleByIdAsync
        public async Task<Schedules?> GetScheduleByIdAsync(int id)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        #endregion

        #region GetScheduleByIdTrackedAsync
        public async Task<Schedules?> GetScheduleByIdTrackedAsync(int id)
        {
            return await context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        #endregion

        #region GetSchedulesByInstructorIdAsync
        public async Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(int instructorId)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .Where(s => s.InstructorId == instructorId)
                .ToListAsync();
        }
        #endregion

        #region GetSchedulesBySectionIdAsync
        public async Task<IEnumerable<Schedules>> GetSchedulesBySectionIdAsync(int sectionId)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .Where(s => s.SectionId == sectionId)
                .ToListAsync();
        }
        #endregion

        #region GetSubjectsByInstructorIdAsync
        public async Task<IEnumerable<Subject>> GetSubjectsByInstructorIdAsync(int instructorId)
        {
            return await context.Schedules
                .AsNoTracking()
                .Where(s => s.InstructorId == instructorId)
                .Select(s => s.Subject)
                .Distinct()
                .OrderBy(subject => subject.Name)
                .ToListAsync();
        }
        #endregion

        #region GetSchedulesBySectionsAndSubjectsAsync
        /// <summary>
        /// Retrieves schedules for multiple sections and subjects.
        /// Used for finding active sessions for a student.
        /// </summary>
        public async Task<IEnumerable<Schedules>> GetSchedulesBySectionsAndSubjectsAsync(
            IEnumerable<int> sectionIds,
            IEnumerable<int> subjectIds)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Section)
                .Include(s => s.Classroom)
                .Include(s => s.Instructor)
                .Where(s => sectionIds.Contains(s.SectionId) && subjectIds.Contains(s.SubjectId))
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