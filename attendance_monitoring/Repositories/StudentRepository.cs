using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.IRepository;

namespace attendance_monitoring.Repositories;

public class StudentRepository(ApplicationDbContext context) : IStudentRepository
{
    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        return await context.Students.ToListAsync();
    }

    public async Task<IEnumerable<Student>> GetAllNonDeletedStudentsAsync()
    {
        return await context.Students.Where(student => !student.IsDeleted).ToListAsync().ConfigureAwait(false);
    }

    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted).ConfigureAwait(false);
    }

    public async Task<Student?> GetStudentByUserIdAsync(string userId)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted).ConfigureAwait(false);
    }

    public async Task<Student> CreateStudent(Student student)
    {
        var entry = await context.Students.AddAsync(student).ConfigureAwait(false);
        return entry.Entity;
    }

    public Task<Student> UpdateStudentAsync(Student student)
    {
        var entry = context.Students.Update(student);
        return Task.FromResult(entry.Entity);
    }

    public async Task<bool> SoftDeleteStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        student.IsDeleted = true;
        student.DeletedAt = DateTime.UtcNow;
        student.UpdatedAt = DateTime.UtcNow;
        
        context.Students.Update(student);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> HardDeleteStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        context.Students.Remove(student);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RestoreStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        student.IsDeleted = false;
        student.DeletedAt = null;
        student.UpdatedAt = DateTime.UtcNow;
        
        context.Students.Update(student);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<Student?> GetStudentByIdIgnoreDeleteStatus(int id)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
