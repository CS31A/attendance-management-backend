using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository
{
    public interface IScheduleRepository : ISaveableRepository
    {
        Task<IEnumerable<Schedules>> GetAllSchedulesAsync();
        Task<Schedules?> GetScheduleByIdAsync(Guid id);
        Task<Schedules?> GetScheduleByIdTrackedAsync(Guid id);
        Task<Schedules?> GetScheduleByUuidAsync(Guid id);
        Task<Schedules?> GetScheduleByUuidTrackedAsync(Guid id);
        Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(Guid instructorId);
        Task<IEnumerable<Schedules>> GetSchedulesBySectionIdAsync(Guid sectionId);
        Task<IEnumerable<Subject>> GetSubjectsByInstructorIdAsync(Guid instructorId);
        Task<Schedules> AddScheduleAsync(Schedules schedule);
        Task<Schedules?> UpdateScheduleAsync(Schedules schedule);
        Task<bool> DeleteScheduleAsync(Guid id);
        Task<bool> HasSessionsInScheduleAsync(Guid id);

        // New methods for fingerprint attendance
        /// <summary>
        /// Retrieves schedules for multiple sections and subjects.
        /// Used for finding active sessions for a student.
        /// </summary>
        Task<IEnumerable<Schedules>> GetSchedulesBySectionsAndSubjectsAsync(
            IEnumerable<Guid> sectionIds,
            IEnumerable<Guid> subjectIds);
    }
}
