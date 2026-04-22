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
    Task<Instructor?> GetInstructorByIdAsync(int id);

    /// <summary>
    /// Retrieves an instructor by their UUID.
    /// </summary>
    /// <param name="uuid">The instructor UUID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByUuidAsync(Guid uuid);

    /// <summary>
    /// Retrieves an instructor by their ID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdTrackedAsync(int id);

    /// <summary>
    /// Retrieves an instructor by their UUID with change tracking enabled for updates.
    /// </summary>
    /// <param name="uuid">The instructor UUID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByUuidTrackedAsync(Guid uuid);

    /// <summary>
    /// Retrieves an instructor by their ID, ignoring the delete status.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(int id);

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
    Task<bool> SoftDeleteInstructorAsync(int id);

    /// <summary>
    /// Hard deletes an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was hard deleted; otherwise, false.</returns>
    Task<bool> HardDeleteInstructorAsync(int id);

    /// <summary>
    /// Restores a soft deleted instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was restored; otherwise, false.</returns>
    Task<bool> RestoreInstructorAsync(int id);

    /// <summary>
    /// Retrieves all schedules with related data (Section, Course, Subject, Classroom, Students, StudentEnrollments) for a specific instructor.
    /// </summary>
    /// <param name="instructorId">The instructor ID.</param>
    /// <returns>A collection of schedules with eagerly loaded related entities.</returns>
    Task<IEnumerable<Schedules>> GetSchedulesWithRelatedDataByInstructorIdAsync(int instructorId);

}
