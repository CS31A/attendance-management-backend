using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.IRepository;

namespace attendance_monitoring.Repositories;

public class StudentRepository(ApplicationDbContext context) : IStudentRepository
{

    #region GetAllStudentsAsync
    public async Task<IList<Student>> GetAllStudentsAsync()
    {
        return await context.Students
            .Include(s => s.User)
            .AsNoTracking()
            .ToListAsync();
    }
    #endregion

    #region GetAllNonDeletedStudentsAsync
    public async Task<IList<Student>> GetAllNonDeletedStudentsAsync()
    {
        return await context.Students
            .Include(s => s.User)
            .AsNoTracking()
            .Where(student => !student.IsDeleted)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdAsync
    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await context.Students
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdTrackedAsync
    public async Task<Student?> GetStudentByIdTrackedAsync(int id)
    {
        return await context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByUserIdAsync
    public async Task<Student?> GetStudentByUserIdAsync(string userId)
    {
        return await context.Students
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdIgnoreDeleteStatus
    public async Task<Student?> GetStudentByIdIgnoreDeleteStatus(int id)
    {
        return await context.Students
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id)
            .ConfigureAwait(false);
    }
    #endregion

    #region CreateStudent
    public async Task<Student> CreateStudent(Student student)
    {
        var entry = await context.Students.AddAsync(student).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateStudentAsync
    public Task<Student> UpdateStudentAsync(Student student)
    {
        var entry = context.Students.Update(student);
        return Task.FromResult(entry.Entity);
    }
    #endregion

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

    #region SaveChangesAsync
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetStudentSubjectsAsync
    public async Task<IEnumerable<(Subject Subject, Schedules Schedule, Instructor Instructor, Classroom Classroom)>> GetStudentSubjectsAsync(string userId)
    {
        return await context.Students
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Join(context.Schedules,
                student => student.SectionId,
                schedule => schedule.SectionId,
                (student, schedule) => schedule)
            .Include(schedule => schedule.Subject)
            .Include(schedule => schedule.Instructor)
                .ThenInclude(i => i.User)
            .Include(schedule => schedule.Classroom)
            .Select(schedule => new ValueTuple<Subject, Schedules, Instructor, Classroom>(
                schedule.Subject,
                schedule,
                schedule.Instructor,
                schedule.Classroom))
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion
}
