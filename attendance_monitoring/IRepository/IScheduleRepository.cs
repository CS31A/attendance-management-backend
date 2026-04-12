using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository
{
    public interface IScheduleRepository : ISaveableRepository
    {
        Task<IEnumerable<Schedules>> GetAllSchedulesAsync();
        Task<Schedules?> GetScheduleByIdAsync(int id);
        Task<Schedules?> GetScheduleByIdTrackedAsync(int id);
        Task<IEnumerable<Schedules>> GetSchedulesByInstructorIdAsync(int instructorId);
        Task<IEnumerable<Schedules>> GetSchedulesBySectionIdAsync(int sectionId);
        Task<IEnumerable<Subject>> GetSubjectsByInstructorIdAsync(int instructorId);
        Task<Schedules> AddScheduleAsync(Schedules schedule);
        Task<Schedules?> UpdateScheduleAsync(Schedules schedule);
        Task<bool> DeleteScheduleAsync(int id);

        // New methods for fingerprint attendance
        /// <summary>
        /// Retrieves schedules for multiple sections and subjects.
        /// Used for finding active sessions for a student.
        /// </summary>
        Task<IEnumerable<Schedules>> GetSchedulesBySectionsAndSubjectsAsync(
            IEnumerable<int> sectionIds,
            IEnumerable<int> subjectIds);
    }
}