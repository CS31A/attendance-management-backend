using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class InstructorRepository(ApplicationDbContext context) : IInstructorRepository
{
    #region Read Operations

    #region GetAllInstructorsAsync
    /// <summary>
    /// Retrieves all non-deleted instructors.
    /// Performance: Single query, no navigation properties loaded.
    /// Note: User navigation property has [JsonIgnore] and is not needed for API responses.
    /// </summary>
    public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync()
    {
        return await context.Instructors
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.Id)
            .ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdAsync
    /// <summary>
    /// Retrieves an instructor by ID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdAsync(int id)
    {
        return await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUuidAsync
    /// <summary>
    /// Retrieves an instructor by UUID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUuidAsync(Guid uuid)
    {
        return await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Uuid == uuid && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdTrackedAsync
    /// <summary>
    /// Retrieves an instructor by ID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdTrackedAsync(int id)
    {
        return await context.Instructors
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUuidTrackedAsync
    /// <summary>
    /// Retrieves an instructor by UUID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUuidTrackedAsync(Guid uuid)
    {
        return await context.Instructors
            .FirstOrDefaultAsync(i => i.Uuid == uuid && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUserIdAsync
    /// <summary>
    /// Retrieves an instructor by their Identity User ID.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
    {
        return await context.Instructors
            .AsNoTracking()
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdIgnoreDeleteStatus
    /// <summary>
    /// Retrieves an instructor by ID regardless of delete status.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(int id)
    {
        return await context.Instructors
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

    #region Specialized Query Operations

    #region GetSchedulesWithRelatedDataByInstructorIdAsync
    /// <summary>
    /// Retrieves all schedules with related data for a specific instructor.
    /// Uses eager loading to minimize database round trips (N+1 query prevention).
    /// Includes: Schedule → Section → Course, Subject, Classroom, StudentEnrollments (with Students)
    /// Note: Regular students (Student.SectionId matches) must be queried separately in the service layer.
    /// Note: Soft-delete filtering is NOT applied to Schedules in this query.
    /// Performance: Single query with multiple joins.
    /// </summary>
    public async Task<IEnumerable<Schedules>> GetSchedulesWithRelatedDataByInstructorIdAsync(int instructorId)
    {
        return await context.Schedules
            .AsNoTracking()
            .Where(s => s.InstructorId == instructorId)
            .Include(s => s.Section)
                .ThenInclude(sec => sec.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .Include(s => s.Section.StudentEnrollments.Where(se => se.IsActive))
                .ThenInclude(se => se.Student)
            .ToListAsync()
            .ConfigureAwait(false);
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
