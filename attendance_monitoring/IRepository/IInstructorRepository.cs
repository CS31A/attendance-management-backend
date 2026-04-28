using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing instructors.
/// </summary>
public interface IInstructorRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves all instructors.
    /// </summary>
    /// <returns>A collection of all instructors.</returns>
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync();

    /// <summary>
    /// Retrieves an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdAsync(Guid id);

    /// <summary>
    /// Retrieves an instructor by their UUID.
    /// </summary>
    /// <param name="id">The instructor UUID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByUuidAsync(Guid id);

    /// <summary>
    /// Retrieves an instructor by their ID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdTrackedAsync(Guid id);

    /// <summary>
    /// Retrieves an instructor by their UUID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The instructor UUID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByUuidTrackedAsync(Guid id);

    /// <summary>
    /// Retrieves an instructor by their ID, ignoring the delete status.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(Guid id);

    /// <summary>
    /// Retrieves an instructor by their user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByUserIdAsync(string userId);

    /// <summary>
    /// Creates a new instructor.
    /// </summary>
    /// <param name="instructor">The instructor to create.</param>
    /// <returns>The created instructor.</returns>
    Task<Instructor> CreateInstructorAsync(Instructor instructor);

    /// <summary>
    /// Updates an existing instructor.
    /// </summary>
    /// <param name="instructor">The instructor to update.</param>
    /// <returns>The updated instructor.</returns>
    Task<Instructor> UpdateInstructorAsync(Instructor instructor);

    /// <summary>
    /// Soft deletes an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was softly deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteInstructorAsync(Guid id);

    /// <summary>
    /// Hard deletes an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was hard deleted; otherwise, false.</returns>
    Task<bool> HardDeleteInstructorAsync(Guid id);

    /// <summary>
    /// Restores a soft deleted instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was restored; otherwise, false.</returns>
    Task<bool> RestoreInstructorAsync(Guid id);

    /// <summary>
    /// Retrieves all schedules with related data (Section, Course, Subject, Classroom, Students, StudentEnrollments) for a specific instructor.
    /// </summary>
    /// <param name="instructorId">The instructor ID.</param>
    /// <returns>A collection of schedules with eagerly loaded related entities.</returns>
    Task<IEnumerable<Schedules>> GetSchedulesWithRelatedDataByInstructorIdAsync(Guid instructorId);

    /// <summary>
    /// Retrieves regular students whose primary section matches the supplied section.
    /// </summary>
    /// <param name="sectionId">The section ID.</param>
    /// <returns>A collection of regular students in the section.</returns>
    Task<IEnumerable<Student>> GetRegularStudentsBySectionIdAsync(Guid sectionId);

    /// <summary>
    /// Retrieves all sections handled by the instructor, including course data.
    /// </summary>
    /// <param name="instructorId">The instructor ID.</param>
    /// <returns>A collection of sections handled by the instructor.</returns>
    Task<IEnumerable<Section>> GetHandledSectionsByInstructorIdAsync(Guid instructorId);

    /// <summary>
    /// Retrieves handled classes for a specific section and instructor with related data.
    /// </summary>
    /// <param name="sectionId">The section ID.</param>
    /// <param name="instructorId">The instructor ID.</param>
    /// <returns>A collection of schedules for the supplied section and instructor.</returns>
    Task<IEnumerable<Schedules>> GetHandledClassesBySectionAndInstructorAsync(Guid sectionId, Guid instructorId);

    /// <summary>
    /// Retrieves all non-deleted students whose home section matches the supplied section.
    /// </summary>
    /// <param name="sectionId">The section ID.</param>
    /// <returns>A collection of home section students.</returns>
    Task<IEnumerable<Student>> GetHomeSectionStudentsAsync(Guid sectionId);

    /// <summary>
    /// Determines whether the instructor handles the supplied section.
    /// </summary>
    /// <param name="instructorId">The instructor ID.</param>
    /// <param name="sectionId">The section ID.</param>
    /// <returns><c>true</c> if the instructor handles the section; otherwise, <c>false</c>.</returns>
    Task<bool> IsInstructorHandlingSectionAsync(Guid instructorId, Guid sectionId);

    /// <summary>
    /// Retrieves a student with related section, course, and enrollment data.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>The student with related details if found; otherwise, null.</returns>
    Task<Student?> GetStudentWithDetailsAsync(Guid studentId);

    /// <summary>
    /// Retrieves attendance records for a student in sessions taught by the supplied instructor.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <param name="instructorId">The instructor ID.</param>
    /// <returns>A collection of attendance records for instructor-taught subjects.</returns>
    Task<IEnumerable<AttendanceRecord>> GetStudentAttendanceForInstructorSubjectsAsync(Guid studentId, Guid instructorId);

}
