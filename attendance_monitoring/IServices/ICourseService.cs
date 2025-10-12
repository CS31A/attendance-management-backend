using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface ICourseService
{
    Task<IEnumerable<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseByIdAsync(int id);
    Task<Course> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user);
    Task<Course> UpdateCourseAsync(int id, UpdateCourse updateCourse, ClaimsPrincipal user);
    Task DeleteCourseAsync(int id, ClaimsPrincipal user);
}