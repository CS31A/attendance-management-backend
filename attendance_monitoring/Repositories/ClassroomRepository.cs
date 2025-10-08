using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class ClassroomRepository(ApplicationDbContext context) : IClassroomRepository
{
    public async Task<IEnumerable<Classroom>> GetAllClassroomsAsync()
    {
        return await context.Classrooms.ToListAsync().ConfigureAwait(false);
    }

    public async Task<Classroom?> GetClassroomByIdAsync(int id)
    {
        return await context.Classrooms.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<Classroom> CreateClassroom(Classroom classroom)
    {
        var entry = await context.Classrooms.AddAsync(classroom).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<Classroom> UpdateClassroomAsync(Classroom classroom)
    {
        classroom.UpdatedAt = DateTime.UtcNow;
        context.Classrooms.Update(classroom);
        return classroom;
    }

    public async Task<bool> DeleteClassroomAsync(int id)
    {
        var classroom = await context.Classrooms.FindAsync(id).ConfigureAwait(false);
        if (classroom == null) return false;
        
        context.Classrooms.Remove(classroom);
        return true;
    }

    public async Task<Classroom?> GetClassroomByNameAsync(string name)
    {
        return await context.Classrooms.FirstOrDefaultAsync(c => c.Name == name).ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
}