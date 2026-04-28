using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing subjects.
/// </summary>
public interface ISubjectRepository : ISaveableRepository
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
    Task<Subject?> GetSubjectByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a subject by its UUID.
    /// </summary>
    /// <param name="id">The subject UUID.</param>
    /// <returns>The subject if found; otherwise, null.</returns>
    Task<Subject?> GetSubjectByUuidAsync(Guid id);

    /// <summary>
    /// Retrieves a subject by its ID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The subject ID.</param>
    /// <returns>The subject if found; otherwise, null.</returns>
    Task<Subject?> GetSubjectByIdTrackedAsync(Guid id);

    /// <summary>
    /// Retrieves a subject by its UUID with change tracking enabled for updates.
    /// </summary>
    /// <param name="id">The subject UUID.</param>
    /// <returns>The subject if found; otherwise, null.</returns>
    Task<Subject?> GetSubjectByUuidTrackedAsync(Guid id);

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
    Task<bool> DeleteSubjectAsync(Guid id);

    /// <summary>
    /// Checks if schedules reference the subject.
    /// </summary>
    /// <param name="id">The subject ID.</param>
    /// <returns>True if schedules exist for the subject; otherwise, false.</returns>
    Task<bool> HasSchedulesInSubjectAsync(Guid id);

    /// <summary>
    /// Checks if student enrollments reference the subject.
    /// </summary>
    /// <param name="id">The subject ID.</param>
    /// <returns>True if enrollments exist for the subject; otherwise, false.</returns>
    Task<bool> HasEnrollmentsInSubjectAsync(Guid id);

    /// <summary>
    /// Gets a subject by its code.
    /// </summary>
    /// <param name="code">The subject code.</param>
    /// <returns>The subject if found; otherwise, null.</returns>
    Task<Subject?> GetSubjectByCodeAsync(string code);

}
