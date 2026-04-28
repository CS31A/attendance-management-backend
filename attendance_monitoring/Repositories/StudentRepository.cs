using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.IRepository;

namespace attendance_monitoring.Repositories;

public class StudentRepository(ApplicationDbContext context) : IStudentRepository
{

    #region GetAllStudentsAsync
    /// <summary>
    /// Retrieves all students.
    /// Performance: Single query, no navigation properties loaded.
    /// Note: User navigation property has [JsonIgnore] and is not needed for API responses.
    /// </summary>
    public async Task<IList<Student>> GetAllStudentsAsync()
    {
        return await context.Students
            .AsNoTracking()
            .ToListAsync();
    }
    #endregion

    #region GetAllNonDeletedStudentsAsync
    /// <summary>
    /// Retrieves all non-deleted students as lightweight DTOs.
    /// Performance: Uses database projection - only SELECTs needed columns.
    /// </summary>
    public async Task<IList<StudentListDto>> GetAllNonDeletedStudentsAsync()
    {
        return await context.Students
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                Firstname = s.Firstname,
                Lastname = s.Lastname,
                IsRegular = s.IsRegular,
                UserId = s.UserId,
                SectionId = s.SectionId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsDeleted = s.IsDeleted,
                DeletedAt = s.DeletedAt
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdAsync
    /// <summary>
    /// Retrieves a student by ID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByIdAsync(Guid id)
    {
        return await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByUuidAsync
    /// <summary>
    /// Retrieves a student by UUID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByUuidAsync(Guid id)
    {
        return await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdTrackedAsync
    /// <summary>
    /// Retrieves a student by ID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByIdTrackedAsync(Guid id)
    {
        return await context.Students
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByUuidTrackedAsync
    /// <summary>
    /// Retrieves a student by UUID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByUuidTrackedAsync(Guid id)
    {
        return await context.Students
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByUserIdAsync
    /// <summary>
    /// Retrieves a student by their Identity User ID.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByUserIdAsync(string userId)
    {
        return await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentByIdIgnoreDeleteStatus
    /// <summary>
    /// Retrieves a student by ID regardless of delete status.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Student?> GetStudentByIdIgnoreDeleteStatus(Guid id)
    {
        return await context.Students
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
    public async Task<bool> SoftDeleteStudentAsync(Guid id)
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
    public async Task<bool> HardDeleteStudentAsync(Guid id)
    {
        var student = await context.Students.FindAsync(id).ConfigureAwait(false);
        if (student == null)
            return false;

        context.Students.Remove(student);
        return true;
    }
    #endregion

    #region RestoreStudentAsync
    public async Task<bool> RestoreStudentAsync(Guid id)
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

    #region SearchStudentsByNameAsync
    /// <summary>
    /// Searches students by name with pagination, returning lightweight DTOs.
    /// Performance: Uses database projection for optimal performance.
    /// </summary>
    public async Task<IEnumerable<StudentListDto>> SearchStudentsByNameAsync(string searchTerm, int pageNumber, int pageSize)
    {
        return await context.Students
            .AsNoTracking()
            .Where(s => !s.IsDeleted &&
                   (EF.Functions.Like(s.Firstname, $"%{searchTerm}%") ||
                    EF.Functions.Like(s.Lastname, $"%{searchTerm}%")))
            .OrderBy(s => s.Lastname)
            .ThenBy(s => s.Firstname)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                Firstname = s.Firstname,
                Lastname = s.Lastname,
                IsRegular = s.IsRegular,
                UserId = s.UserId,
                SectionId = s.SectionId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsDeleted = s.IsDeleted,
                DeletedAt = s.DeletedAt
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region SearchStudentsByEmailAsync
    /// <summary>
    /// Searches students by email with pagination, returning lightweight DTOs.
    /// Performance: Uses database projection - User accessed only for filtering/ordering.
    /// </summary>
    public async Task<IEnumerable<StudentListDto>> SearchStudentsByEmailAsync(string searchTerm, int pageNumber, int pageSize)
    {
        return await context.Students
            .AsNoTracking()
            .Where(s => !s.IsDeleted &&
                   s.User != null &&
                   EF.Functions.Like(s.User.Email!, $"%{searchTerm}%"))
            .OrderBy(s => s.User!.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                Firstname = s.Firstname,
                Lastname = s.Lastname,
                IsRegular = s.IsRegular,
                UserId = s.UserId,
                SectionId = s.SectionId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsDeleted = s.IsDeleted,
                DeletedAt = s.DeletedAt
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion
}
