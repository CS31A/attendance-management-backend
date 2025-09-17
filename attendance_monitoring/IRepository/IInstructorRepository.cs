using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing instructors.
/// </summary>
public interface IInstructorRepository
{
    /// <summary>
    /// Retrieves all instructors.
    /// </summary>
    /// <param name="paginationQuery">The pagination query.</param>
    /// <returns>A collection of all instructors.</returns>
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery);

    /// <summary>
    /// Retrieves an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>The instructor if found; otherwise, null.</returns>
    Task<Instructor?> GetInstructorByIdAsync(int id);

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
    Task<Instructor> CreateInstructor(Instructor instructor);

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
    /// <returns>True if the instructor was soft deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteInstructor(int id);

    /// <summary>
    /// Hard deletes an instructor by their ID.
    /// </summary>
    /// <param name="id">The instructor ID.</param>
    /// <returns>True if the instructor was hard deleted; otherwise, false.</returns>
    Task<bool> HardDeleteInstructor(int id);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync();
}