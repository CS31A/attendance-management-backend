using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing courses.
/// </summary>
public interface ICourseRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves all courses.
    /// </summary>
    /// <returns>A collection of all courses.</returns>
    Task<IEnumerable<Course>> GetAllCoursesAsync();

    /// <summary>
    /// Retrieves a course by its ID.
    /// </summary>
    /// <param name="id">The course ID.</param>
    /// <returns>The course if found; otherwise, null.</returns>
    Task<Course?> GetCourseByIdAsync(int id);

    /// <summary>
    /// Creates a new course.
    /// </summary>
    /// <param name="course">The course to create.</param>
    /// <returns>The created course.</returns>
    Task<Course> CreateCourse(Course course);

    /// <summary>
    /// Updates an existing course.
    /// </summary>
    /// <param name="course">The course to update.</param>
    /// <returns>The updated course.</returns>
    Task<Course> UpdateCourseAsync(Course course);

    /// <summary>
    /// Deletes a course by its ID.
    /// </summary>
    /// <param name="id">The course ID.</param>
    /// <returns>True if the course was deleted; otherwise, false.</returns>
    Task<bool> DeleteCourseAsync(int id);

    /// <summary>
    /// Checks if the course has sections assigned.
    /// </summary>
    /// <param name="id">The course ID.</param>
    /// <returns>True if sections exist for the course; otherwise, false.</returns>
    Task<bool> HasSectionsInCourseAsync(int id);

}
