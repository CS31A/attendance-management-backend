using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing classrooms.
/// </summary>
public interface IClassroomRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves all classrooms.
    /// </summary>
    /// <returns>A collection of all classrooms.</returns>
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync();

    /// <summary>
    /// Retrieves a classroom by its ID.
    /// </summary>
    /// <param name="id">The classroom ID.</param>
    /// <returns>The classroom if found; otherwise, null.</returns>
    Task<Classroom?> GetClassroomByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a classroom by its UUID.
    /// </summary>
    /// <param name="id">The classroom UUID.</param>
    /// <returns>The classroom if found; otherwise, null.</returns>
    Task<Classroom?> GetClassroomByUuidAsync(Guid id);

    /// <summary>
    /// Retrieves a classroom by its UUID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The classroom UUID.</param>
    /// <returns>The classroom if found; otherwise, null.</returns>
    Task<Classroom?> GetClassroomByUuidTrackedAsync(Guid id);

    /// <summary>
    /// Creates a new classroom.
    /// </summary>
    /// <param name="classroom">The classroom to create.</param>
    /// <returns>The created classroom.</returns>
    Task<Classroom> CreateClassroom(Classroom classroom);

    /// <summary>
    /// Updates an existing classroom.
    /// </summary>
    /// <param name="classroom">The classroom to update.</param>
    /// <returns>The updated classroom.</returns>
    Task<Classroom> UpdateClassroomAsync(Classroom classroom);

    /// <summary>
    /// Deletes a classroom by its ID.
    /// </summary>
    /// <param name="id">The classroom ID.</param>
    /// <returns>True if the classroom was deleted; otherwise, false.</returns>
    Task<bool> DeleteClassroomAsync(Guid id);

    /// <summary>
    /// Checks if schedules reference the classroom.
    /// </summary>
    /// <param name="id">The classroom ID.</param>
    /// <returns>True if schedules exist for the classroom; otherwise, false.</returns>
    Task<bool> HasSchedulesInClassroomAsync(Guid id);

    /// <summary>
    /// Checks if sessions reference the classroom as an actual room.
    /// </summary>
    /// <param name="id">The classroom ID.</param>
    /// <returns>True if sessions exist for the classroom; otherwise, false.</returns>
    Task<bool> HasSessionsInClassroomAsync(Guid id);

    /// <summary>
    /// Gets a classroom by its name.
    /// </summary>
    /// <param name="name">The classroom name.</param>
    /// <returns>The classroom if found; otherwise, null.</returns>
    Task<Classroom?> GetClassroomByNameAsync(string name);

}
