using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class ClassroomRepository(ApplicationDbContext context) : IClassroomRepository
{
    #region Read Operations

    #region GetAllClassroomsAsync
    public async Task<IEnumerable<Classroom>> GetAllClassroomsAsync()
    {
        return await context.Classrooms.AsNoTracking().ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetClassroomByIdAsync
    public async Task<Classroom?> GetClassroomByIdAsync(int id)
    {
        return await context.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
    }
    #endregion

    #region GetClassroomByNameAsync
    public async Task<Classroom?> GetClassroomByNameAsync(string name)
    {
        return await context.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateClassroom
    public async Task<Classroom> CreateClassroom(Classroom classroom)
    {
        var entry = await context.Classrooms.AddAsync(classroom).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateClassroomAsync
    public Task<Classroom> UpdateClassroomAsync(Classroom classroom)
    {
        classroom.UpdatedAt = DateTime.UtcNow;
        context.Classrooms.Update(classroom);
        return Task.FromResult(classroom);
    }
    #endregion

    #region DeleteClassroomAsync
    public async Task<bool> DeleteClassroomAsync(int id)
    {
        var classroom = await context.Classrooms.FindAsync(id).ConfigureAwait(false);
        if (classroom == null) return false;

        context.Classrooms.Remove(classroom);
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