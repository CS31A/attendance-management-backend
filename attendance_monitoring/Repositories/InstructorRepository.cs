using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class InstructorRepository(ApplicationDbContext context) : IInstructorRepository
{
    public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync()
    {
        return await context.Instructors
            .Where(i => !i.IsDeleted) // Only return non-deleted instructors
            .OrderBy(i => i.Id)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<Instructor?> GetInstructorByIdAsync(int id)
    {
        return await context.Instructors.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted).ConfigureAwait(false);
    }

    public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
    {
        return await context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted).ConfigureAwait(false);
    }

    public async Task<Instructor> CreateInstructorAsync(Instructor instructor)
    {
        var entry = await context.Instructors.AddAsync(instructor).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<Instructor> UpdateInstructorAsync(Instructor instructor)
    {
        instructor.UpdatedAt = DateTime.UtcNow;
        var entry = context.Instructors.Update(instructor);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<bool> SoftDeleteInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = true;
        instructor.DeletedAt = DateTime.UtcNow;
        instructor.UpdatedAt = DateTime.UtcNow;
        
        context.Instructors.Update(instructor);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> HardDeleteInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        context.Instructors.Remove(instructor);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
