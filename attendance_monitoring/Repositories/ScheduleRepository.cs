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
                    .ThenInclude(section => section.Course)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .ToListAsync();
        }
        #endregion

        #region GetScheduleByIdAsync
        public async Task<Schedules?> GetScheduleByIdAsync(Guid id)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                    .ThenInclude(section => section.Course)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        #endregion

        #region GetScheduleByIdTrackedAsync
        public async Task<Schedules?> GetScheduleByIdTrackedAsync(Guid id)
        {
            return await context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                    .ThenInclude(section => section.Course)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        #endregion

        #region GetScheduleByUuidAsync
        public async Task<Schedules?> GetScheduleByUuidAsync(Guid id)
        {
            var scheduleId = await context.Schedules
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => (Guid?)s.Id)
                .SingleOrDefaultAsync();

            return scheduleId.HasValue
                ? await GetScheduleByIdAsync(scheduleId.Value)
                : null;
        }
        #endregion

        #region GetScheduleByUuidTrackedAsync
        public async Task<Schedules?> GetScheduleByUuidTrackedAsync(Guid id)
        {
            var scheduleId = await context.Schedules
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => (Guid?)s.Id)
                .SingleOrDefaultAsync();

            return scheduleId.HasValue
                ? await GetScheduleByIdTrackedAsync(scheduleId.Value)
                : null;
        }
        #endregion

        #region GetSchedulesByInstructorIdAsync
        public async Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(Guid instructorId)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                    .ThenInclude(section => section.Course)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .Where(s => s.InstructorId == instructorId)
                .ToListAsync();
        }
        #endregion

        #region GetSchedulesBySectionIdAsync
        public async Task<IEnumerable<Schedules>> GetSchedulesBySectionIdAsync(Guid sectionId)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                    .ThenInclude(section => section.Course)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .Where(s => s.SectionId == sectionId)
                .ToListAsync();
        }
        #endregion

        #region GetSubjectsByInstructorIdAsync
        public async Task<IEnumerable<Subject>> GetSubjectsByInstructorIdAsync(Guid instructorId)
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
            IEnumerable<Guid> sectionIds,
            IEnumerable<Guid> subjectIds)
        {
            return await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Section)
                    .ThenInclude(section => section.Course)
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
        public async Task<bool> DeleteScheduleAsync(Guid id)
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

        #region HasSessionsInScheduleAsync
        public async Task<bool> HasSessionsInScheduleAsync(Guid id)
        {
            return await context.Sessions
                .AsNoTracking()
                .AnyAsync(session => session.ScheduleId == id)
                .ConfigureAwait(false);
        }
        #endregion

        #region Overlap Checks
        public Task<ScheduleConflictDetails?> FindClassroomOverlapAsync(
            Guid classroomId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId = null)
        {
            return FindOverlapAsync(
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                schedule => schedule.ClassroomId == classroomId);
        }

        public Task<ScheduleConflictDetails?> FindInstructorOverlapAsync(
            Guid instructorId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId = null)
        {
            return FindOverlapAsync(
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                schedule => schedule.InstructorId == instructorId);
        }

        public Task<ScheduleConflictDetails?> FindSectionOverlapAsync(
            Guid sectionId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId = null)
        {
            return FindOverlapAsync(
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                schedule => schedule.SectionId == sectionId);
        }

        private Task<ScheduleConflictDetails?> FindOverlapAsync(
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId,
            System.Linq.Expressions.Expression<Func<Schedules, bool>> resourcePredicate)
        {
            return context.Schedules
                .AsNoTracking()
                .Where(resourcePredicate)
                .Where(schedule => schedule.DayOfWeek == dayOfWeek)
                .Where(schedule => schedule.TimeIn < timeOut && timeIn < schedule.TimeOut)
                .Where(schedule => !excludedScheduleId.HasValue || schedule.Id != excludedScheduleId.Value)
                .OrderBy(schedule => schedule.TimeIn)
                .Select(schedule => new ScheduleConflictDetails
                {
                    ScheduleId = schedule.Id,
                    DayOfWeek = schedule.DayOfWeek,
                    TimeIn = schedule.TimeIn,
                    TimeOut = schedule.TimeOut,
                    SubjectName = schedule.Subject.Name,
                    ClassroomName = schedule.Classroom.Name,
                    SectionName = schedule.Section.Name,
                    InstructorName = schedule.Instructor.Firstname + " " + schedule.Instructor.Lastname,
                })
                .FirstOrDefaultAsync();
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
