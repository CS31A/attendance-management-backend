using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IRepository;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllCoursesAsync(PaginationQuery paginationQuery);
    Task<Course?> GetCourseByIdAsync(int id);
    Task<Course> CreateCourse(Course course);
    Task<Course> UpdateCourseAsync(Course course);
    Task<bool> DeleteCourseAsync(int id);
    Task<int> SaveChangesAsync();
}