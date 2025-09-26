using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class InstructorRepository(ApplicationDbContext context) : IInstructorRepository
{

    #region Read Operations

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

    public async Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(int id)
    {
        return await context.Instructors.FirstOrDefaultAsync(i => i.Id == id).ConfigureAwait(false);
    }

    #endregion

    #region Create Operations

    public async Task<Instructor> CreateInstructorAsync(Instructor instructor)
    {
        var entry = await context.Instructors.AddAsync(instructor).ConfigureAwait(false);
        return entry.Entity;
    }

    #endregion

    #region Update Operations

    public async Task<Instructor> UpdateInstructorAsync(Instructor instructor)
    {
        instructor.UpdatedAt = DateTime.UtcNow;
        var entry = context.Instructors.Update(instructor);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return entry.Entity;
    }

    #endregion

    #region Delete Operations

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

    public async Task<bool> RestoreInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = false;
        instructor.DeletedAt = null;
        instructor.UpdatedAt = DateTime.UtcNow;
        
        context.Instructors.Update(instructor);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    #endregion

    #region Utility Operations

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }

    #endregion
}
