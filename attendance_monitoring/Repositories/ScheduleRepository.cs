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
            return FindOverlapWithLockAsync(
                classroomId,
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                "ClassroomId");
        }

        public Task<ScheduleConflictDetails?> FindInstructorOverlapAsync(
            Guid instructorId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId = null)
        {
            return FindOverlapWithLockAsync(
                instructorId,
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                "InstructorId");
        }

        public Task<ScheduleConflictDetails?> FindSectionOverlapAsync(
            Guid sectionId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId = null)
        {
            return FindOverlapWithLockAsync(
                sectionId,
                dayOfWeek,
                timeIn,
                timeOut,
                excludedScheduleId,
                "SectionId");
        }

        /// <summary>
        /// Finds the first overlapping schedule for a given resource (classroom, instructor, or section)
        /// using UPDLOCK + HOLDLOCK to prevent TOCTOU race conditions under concurrent writes.
        /// </summary>
        /// <remarks>
        /// UPDLOCK acquires update locks (incompatible with other UPDLOCKs) so concurrent
        /// transactions block instead of deadlocking. HOLDLOCK holds range locks until
        /// transaction commit, preventing phantom inserts in the checked time range.
        /// See: https://michaeljswart.com/2011/09/mythbusting-concurrent-updateinsert-solutions/
        /// </remarks>
        private async Task<ScheduleConflictDetails?> FindOverlapWithLockAsync(
            Guid resourceId,
            string dayOfWeek,
            TimeOnly timeIn,
            TimeOnly timeOut,
            Guid? excludedScheduleId,
            string predicateColumn)
        {
            if (!context.Database.IsSqlServer())
            {
                return await FindOverlapAsync(
                    dayOfWeek,
                    timeIn,
                    timeOut,
                    excludedScheduleId,
                    predicateColumn switch
                    {
                        "ClassroomId" => schedule => schedule.ClassroomId == resourceId,
                        "InstructorId" => schedule => schedule.InstructorId == resourceId,
                        "SectionId" => schedule => schedule.SectionId == resourceId,
                        _ => throw new ArgumentOutOfRangeException(nameof(predicateColumn), predicateColumn, "Unsupported schedule overlap predicate."),
                    }).ConfigureAwait(false);
            }

            // Step 1: Lock the range and find the conflicting schedule ID in a single round-trip.
            // The WITH (UPDLOCK, HOLDLOCK) hint serializes concurrent writers on the same
            // (resource, day, time-range) without the deadlock risk of SERIALIZABLE isolation.
            var excludedScheduleParameter = excludedScheduleId.HasValue
                ? excludedScheduleId.Value
                : (object)DBNull.Value;
            var overlapSql = $@"
                SELECT TOP 1 s.Id AS Value
                FROM Schedules s WITH (UPDLOCK, HOLDLOCK)
                WHERE s.{predicateColumn} = {{0}}
                  AND s.DayOfWeek = {{1}}
                  AND s.TimeIn < {{2}} AND {{3}} < s.TimeOut
                  AND ({{4}} IS NULL OR s.Id != {{4}})
                ORDER BY s.TimeIn";
            var conflictingId = await context.Database
                .SqlQueryRaw<Guid>(
                    overlapSql,
                    resourceId,
                    dayOfWeek,
                    timeOut,
                    timeIn,
                    excludedScheduleParameter)
                .FirstOrDefaultAsync();

            if (conflictingId == Guid.Empty)
            {
                return null;
            }

            // Step 2: Retrieve full conflict details with navigation properties.
            // This is a fast PK lookup; the range lock from Step 1 is still held
            // because both queries execute within the same transaction.
            return await GetScheduleConflictDetailsAsync(conflictingId);
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

        private async Task<ScheduleConflictDetails?> GetScheduleConflictDetailsAsync(Guid scheduleId)
        {
            var schedule = await context.Schedules
                .AsNoTracking()
                .Include(s => s.Subject)
                .Include(s => s.Classroom)
                .Include(s => s.Section)
                .Include(s => s.Instructor)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(s => s.Id == scheduleId)
                .ConfigureAwait(false);

            if (schedule == null)
            {
                return null;
            }

            return new ScheduleConflictDetails
            {
                ScheduleId = schedule.Id,
                DayOfWeek = schedule.DayOfWeek,
                TimeIn = schedule.TimeIn,
                TimeOut = schedule.TimeOut,
                SubjectName = schedule.Subject?.Name,
                ClassroomName = schedule.Classroom?.Name,
                SectionName = schedule.Section?.Name,
                InstructorName = schedule.Instructor != null
                    ? $"{schedule.Instructor.Firstname} {schedule.Instructor.Lastname}"
                    : null,
            };
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
