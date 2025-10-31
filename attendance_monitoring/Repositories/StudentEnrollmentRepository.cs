using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class StudentEnrollmentRepository : IStudentEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public StudentEnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StudentEnrollment>> GetAllAsync()
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .ToListAsync();
    }

    public async Task<StudentEnrollment?> GetByIdAsync(int id)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .FirstOrDefaultAsync(se => se.Id == id);
    }

    public async Task<StudentEnrollment> CreateAsync(StudentEnrollment enrollment)
    {
        enrollment.CreatedAt = DateTime.UtcNow;
        enrollment.UpdatedAt = DateTime.UtcNow;
        enrollment.EnrolledAt = DateTime.UtcNow;

        _context.StudentEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return await GetByIdAsync(enrollment.Id) ?? enrollment;
    }

    public async Task<StudentEnrollment> UpdateAsync(StudentEnrollment enrollment)
    {
        enrollment.UpdatedAt = DateTime.UtcNow;
        _context.StudentEnrollments.Update(enrollment);
        await _context.SaveChangesAsync();
        return enrollment;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(id);
        if (enrollment == null) return false;

        _context.StudentEnrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(int studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(int sectionId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Subject)
            .Where(se => se.SectionId == sectionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSubjectEnrollmentsAsync(int subjectId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Where(se => se.SubjectId == subjectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(int sectionId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .Where(se => se.SectionId == sectionId && se.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSubjectEnrollmentsAsync(int subjectId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Where(se => se.SubjectId == subjectId && se.IsActive)
            .ToListAsync();
    }

    public async Task<StudentEnrollment?> GetEnrollmentAsync(int studentId, int sectionId, int subjectId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .FirstOrDefaultAsync(se => se.StudentId == studentId &&
                               se.SectionId == sectionId &&
                               se.SubjectId == subjectId);
    }

    public async Task<bool> IsStudentEnrolledAsync(int studentId, int sectionId, int subjectId)
    {
    return await _context.StudentEnrollments
    .AsNoTracking()
    .AnyAsync(se => se.StudentId == studentId &&
    se.SectionId == sectionId &&
    se.SubjectId == subjectId &&
                       se.IsActive);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveEnrollmentsAsync(int studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Section>> GetStudentSectionsAsync(int studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .Select(se => se.Section)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<Subject>> GetStudentSubjectsAsync(int studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .Select(se => se.Subject)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> DeactivateEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(enrollmentId);
        if (enrollment == null) return false;

        enrollment.IsActive = false;
        enrollment.DroppedAt = DateTime.UtcNow;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(enrollmentId);
        if (enrollment == null) return false;

        enrollment.IsActive = true;
        enrollment.DroppedAt = null;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}