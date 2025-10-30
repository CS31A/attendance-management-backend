using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class InstructorRepository(ApplicationDbContext context) : IInstructorRepository
{
    #region Read Operations

    #region GetAllInstructorsAsync
    public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync()
    {
        return await context.Instructors
            .Include(i => i.User)
            .AsNoTracking()
            .Where(i => !i.IsDeleted) // Only return non-deleted instructors
            .OrderBy(i => i.Id)
            .ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdAsync
    public async Task<Instructor?> GetInstructorByIdAsync(int id)
    {
        return await context.Instructors
            .Include(i => i.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUserIdAsync
    public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
    {
        return await context.Instructors
            .Include(i => i.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdIgnoreDeleteStatus
    public async Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(int id)
    {
        return await context.Instructors
            .Include(i => i.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id)
            .ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region Create Operations

    #region CreateInstructorAsync
    public async Task<Instructor> CreateInstructorAsync(Instructor instructor)
    {
        var entry = await context.Instructors.AddAsync(instructor).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #endregion

    #region Update Operations

    #region UpdateInstructorAsync
    public Task<Instructor> UpdateInstructorAsync(Instructor instructor)
    {
        instructor.UpdatedAt = DateTime.UtcNow;
        var entry = context.Instructors.Update(instructor);
        return Task.FromResult(entry.Entity);
    }
    #endregion

    #endregion

    #region Delete Operations

    #region SoftDeleteInstructorAsync
    public async Task<bool> SoftDeleteInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = true;
        instructor.DeletedAt = DateTime.UtcNow;
        instructor.UpdatedAt = DateTime.UtcNow;
        
        context.Instructors.Update(instructor);
        return true;
    }
    #endregion

    #region HardDeleteInstructorAsync
    public async Task<bool> HardDeleteInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        context.Instructors.Remove(instructor);
        return true;
    }
    #endregion

    #region RestoreInstructorAsync
    public async Task<bool> RestoreInstructorAsync(int id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = false;
        instructor.DeletedAt = null;
        instructor.UpdatedAt = DateTime.UtcNow;
        
        context.Instructors.Update(instructor);
        return true;
    }
    #endregion

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
