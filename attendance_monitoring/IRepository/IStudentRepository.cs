using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing students.
/// </summary>
public interface IStudentRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves all students.
    /// </summary>
    /// <returns>A collection of all students.</returns>
    Task<IList<Student>> GetAllStudentsAsync();

    /// <summary>
    /// Retrieves all non-deleted students
    /// </summary>
    Task<IList<Student>> GetAllNonDeletedStudentsAsync();

    /// <summary>
    /// Retrieves a student by their ID.
    /// </summary>
    /// <param name="id">The student ID.</param>
    /// <returns>The student if found; otherwise, null.</returns>
    Task<Student?> GetStudentByIdAsync(int id);

    /// <summary>
    /// Retrieves a student by their user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The student if found; otherwise, null.</returns>
    Task<Student?> GetStudentByUserIdAsync(string userId);

    /// <summary>
    /// Creates a new student.
    /// </summary>
    /// <param name="student">The student to create.</param>
    /// <returns>The created student.</returns>
    Task<Student> CreateStudent(Student student);

    /// <summary>
    /// Updates an existing student.
    /// </summary>
    /// <param name="student">The student to update.</param>
    /// <returns>The updated student.</returns>
    Task<Student> UpdateStudentAsync(Student student);

    /// <summary>
    /// Soft deletes a student by their ID.
    /// </summary>
    /// <param name="id">The student ID.</param>
    /// <returns>True if the student was soft deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteStudentAsync(int id);

    /// <summary>
    /// Hard deletes a student by their ID.
    /// </summary>
    /// <param name="id">The student ID.</param>
    /// <returns>True if the student was hard deleted; otherwise, false.</returns>
    Task<bool> HardDeleteStudentAsync(int id);

    /// <summary>
    /// Restores a soft deleted student by their ID.
    /// </summary>
    /// <param name="id">The student ID.</param>
    /// <returns>True if the student was restored; otherwise, false.</returns>
    Task<bool> RestoreStudentAsync(int id);

    /// <summary>
    /// Retrieves a student by their ID, ignoring the delete status.
    /// </summary>
    /// <param name="id">The student ID.</param>
    /// <returns>The student if found; otherwise, null.</returns>
    Task<Student?> GetStudentByIdIgnoreDeleteStatus(int id);

}