using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.IServices;

public interface ICourseService
{
    Task<IList<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseByIdAsync(Guid id);
    Task<Course> GetCourseByUuidAsync(Guid id);
    Task<Course> CreateCourseAsync(CreateCourse createCourse, ClaimsPrincipal user);
    Task<Course> UpdateCourseAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user);
    Task<Course> UpdateCourseByUuidAsync(Guid id, UpdateCourse updateCourse, ClaimsPrincipal user);
    Task DeleteCourseAsync(Guid id, ClaimsPrincipal user);
    Task DeleteCourseByUuidAsync(Guid id, ClaimsPrincipal user);
    Task<bool> HasSectionsInCourseAsync(Guid id);
}
