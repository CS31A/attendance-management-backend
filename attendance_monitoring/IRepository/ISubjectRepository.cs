using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing subjects.
/// </summary>
public interface ISubjectRepository
{
    /// <summary>
    /// Retrieves all subjects.
    /// </summary>
    /// <returns>A collection of all subjects.</returns>
    Task<IEnumerable<Subject>> GetAllSubjectsAsync();

    /// <summary>
    /// Retrieves a subject by its ID.
    /// </summary>
    /// <param name="id">The subject ID.</param>
    /// <returns>The subject if found; otherwise, null.</returns>
    Task<Subject?> GetSubjectByIdAsync(int id);

    /// <summary>
    /// Creates a new subject.
    /// </summary>
    /// <param name="subject">The subject to create.</param>
    /// <returns>The created subject.</returns>
    Task<Subject> CreateSubject(Subject subject);

    /// <summary>
    /// Updates an existing subject.
    /// </summary>
    /// <param name="subject">The subject to update.</param>
    /// <returns>The updated subject.</returns>
    Task<Subject> UpdateSubjectAsync(Subject subject);

    /// <summary>
    /// Deletes a subject by its ID.
    /// </summary>
    /// <param name="id">The subject ID.</param>
    /// <returns>True if the subject was deleted; otherwise, false.</returns>
    Task<bool> DeleteSubjectAsync(int id);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync();
}