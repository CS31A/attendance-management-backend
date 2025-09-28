using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class SubjectRepository(ApplicationDbContext context) : ISubjectRepository
{
    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        return await context.Subjects.ToListAsync().ConfigureAwait(false);
    }

    public async Task<Subject?> GetSubjectByIdAsync(int id)
    {
        return await context.Subjects.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<Subject> CreateSubject(Subject subject)
    {
        var entry = await context.Subjects.AddAsync(subject).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<Subject> UpdateSubjectAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.UtcNow;
        context.Subjects.Update(subject);
        var rowsAffected = await context.SaveChangesAsync().ConfigureAwait(false);
        
        if (rowsAffected == 0)
        {
            throw new ArgumentException($"Subject with ID {subject.Id} does not exist or may have been updated by another process.");
        }
        
        return subject;
    }

    public async Task<bool> DeleteSubjectAsync(int id)
    {
        var subject = await context.Subjects.FindAsync(id).ConfigureAwait(false);
        if (subject == null) return false;
        
        context.Subjects.Remove(subject);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<Subject?> GetSubjectByCodeAsync(string code)
    {
        return await context.Subjects.FirstOrDefaultAsync(s => s.Code == code).ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
}