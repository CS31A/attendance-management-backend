using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _context;

    public CourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Course>> GetAllCoursesAsync(PaginationQuery paginationQuery)
    {
        return await _context.Courses
            .Skip((paginationQuery.PageNumber - 1) * paginationQuery.PageSize)
            .Take(paginationQuery.PageSize)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _context.Courses.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<Course> CreateCourse(Course course)
    {
        var entry = await _context.Courses.AddAsync(course).ConfigureAwait(false);
        return entry.Entity;
    }

    public Task<Course> UpdateCourseAsync(Course course)
    {
        var entry = _context.Courses.Update(course);
        return Task.FromResult(entry.Entity);
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id).ConfigureAwait(false);
        if (course == null) return false;
        
        _context.Courses.Remove(course);
        return true;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}