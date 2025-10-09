using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class CourseRepository(ApplicationDbContext context) : ICourseRepository
{
    #region Read Operations

    #region GetAllCoursesAsync
    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        return await context.Courses.ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetCourseByIdAsync
    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await context.Courses.FindAsync(id).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateCourse
    public async Task<Course> CreateCourse(Course course)
    {
        var entry = await context.Courses.AddAsync(course).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateCourseAsync
    public Task<Course> UpdateCourseAsync(Course course)
    {
        var entry = context.Courses.Update(course);
        return Task.FromResult(entry.Entity);
    }
    #endregion

    #region DeleteCourseAsync
    public async Task<bool> DeleteCourseAsync(int id)
    {
        var course = await context.Courses.FindAsync(id).ConfigureAwait(false);
        if (course == null) return false;
        
        context.Courses.Remove(course);
        return true;
    }
    #endregion

    #endregion

    #region Utility Operations

    #region SaveChangesAsync
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
    #endregion

    #endregion
}