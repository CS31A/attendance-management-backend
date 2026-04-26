using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class SubjectRepository(ApplicationDbContext context) : ISubjectRepository
{
    #region Read Operations

    #region GetAllSubjectsAsync
    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        return await context.Subjects.AsNoTracking().ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetSubjectByIdAsync
    public async Task<Subject?> GetSubjectByIdAsync(Guid id)
    {
        return await context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
    #endregion

    #region GetSubjectByUuidAsync
    public async Task<Subject?> GetSubjectByUuidAsync(Guid id)
    {
        return await context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
    #endregion

    #region GetSubjectByIdTrackedAsync
    public async Task<Subject?> GetSubjectByIdTrackedAsync(Guid id)
    {
        return await context.Subjects.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
    #endregion

    #region GetSubjectByUuidTrackedAsync
    public async Task<Subject?> GetSubjectByUuidTrackedAsync(Guid id)
    {
        return await context.Subjects.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
    #endregion

    #region GetSubjectByCodeAsync
    public async Task<Subject?> GetSubjectByCodeAsync(string code)
    {
        return await context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateSubject
    public async Task<Subject> CreateSubject(Subject subject)
    {
        var entry = await context.Subjects.AddAsync(subject).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateSubjectAsync
    public Task<Subject> UpdateSubjectAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.UtcNow;
        context.Subjects.Update(subject);
        return Task.FromResult(subject);
    }
    #endregion

    #region DeleteSubjectAsync
    public async Task<bool> DeleteSubjectAsync(Guid id)
    {
        var subject = await context.Subjects.FindAsync(id).ConfigureAwait(false);
        if (subject == null) return false;

        context.Subjects.Remove(subject);
        return true;
    }
    #endregion

    #region HasSchedulesInSubjectAsync
    public async Task<bool> HasSchedulesInSubjectAsync(Guid id)
    {
        return await context.Schedules
            .AsNoTracking()
            .AnyAsync(schedule => schedule.SubjectId == id)
            .ConfigureAwait(false);
    }
    #endregion

    #region HasEnrollmentsInSubjectAsync
    public async Task<bool> HasEnrollmentsInSubjectAsync(Guid id)
    {
        return await context.StudentEnrollments
            .AsNoTracking()
            .AnyAsync(enrollment => enrollment.SubjectId == id)
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
