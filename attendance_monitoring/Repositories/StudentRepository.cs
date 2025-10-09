using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.IRepository;

namespace attendance_monitoring.Repositories;

public class StudentRepository(ApplicationDbContext context) : IStudentRepository
{
    #region Read Operations

    #region GetAllStudentsAsync
    public async Task<IList<Student>> GetAllStudentsAsync()
    {
        return await context.Students.ToListAsync();
    }
    #endregion

    #region GetAllNonDeletedStudentsAsync
    public async Task<IList<Student>> GetAllNonDeletedStudentsAsync()
    {
        return await context.Students.Where(student => !student.IsDeleted).ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdAsync
    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted).ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByUserIdAsync
    public async Task<Student?> GetStudentByUserIdAsync(string userId)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted).ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdIgnoreDeleteStatus
    public async Task<Student?> GetStudentByIdIgnoreDeleteStatus(int id)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region Create Operations

    #region CreateStudent
    public async Task<Student> CreateStudent(Student student)
    {
        var entry = await context.Students.AddAsync(student).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #endregion

    #region Update Operations

    #region UpdateStudentAsync
    public Task<Student> UpdateStudentAsync(Student student)
    {
        var entry = context.Students.Update(student);
        return Task.FromResult(entry.Entity);
    }
    #endregion

    #endregion

    #region Delete Operations

    #region SoftDeleteStudentAsync
    public async Task<bool> SoftDeleteStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        student.IsDeleted = true;
        student.DeletedAt = DateTime.UtcNow;
        student.UpdatedAt = DateTime.UtcNow;
        
        context.Students.Update(student);
        return true;
    }
    #endregion

    #region HardDeleteStudentAsync
    public async Task<bool> HardDeleteStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        context.Students.Remove(student);
        return true;
    }
    #endregion

    #region RestoreStudentAsync
    public async Task<bool> RestoreStudentAsync(int id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        student.IsDeleted = false;
        student.DeletedAt = null;
        student.UpdatedAt = DateTime.UtcNow;
        
        context.Students.Update(student);
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
