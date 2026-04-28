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

    public async Task<StudentEnrollment?> GetByIdAsync(Guid id)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .FirstOrDefaultAsync(se => se.Id == id);
    }

    public async Task<StudentEnrollment?> GetByIdTrackedAsync(Guid id)
    {
        return await _context.StudentEnrollments
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .FirstOrDefaultAsync(se => se.Id == id);
    }

    public async Task<StudentEnrollment?> GetByUuidAsync(Guid id)
    {
        var enrollmentId = await _context.StudentEnrollments
            .AsNoTracking()
            .Where(se => se.Id == id)
            .Select(se => (Guid?)se.Id)
            .SingleOrDefaultAsync();

        return enrollmentId.HasValue
            ? await GetByIdAsync(enrollmentId.Value)
            : null;
    }

    public async Task<StudentEnrollment?> GetByUuidTrackedAsync(Guid id)
    {
        var enrollmentId = await _context.StudentEnrollments
            .AsNoTracking()
            .Where(se => se.Id == id)
            .Select(se => (Guid?)se.Id)
            .SingleOrDefaultAsync();

        return enrollmentId.HasValue
            ? await GetByIdTrackedAsync(enrollmentId.Value)
            : null;
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

    public async Task<bool> DeleteAsync(Guid id)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(id);
        if (enrollment == null) return false;

        _context.StudentEnrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(Guid studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(Guid sectionId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Subject)
            .Where(se => se.SectionId == sectionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSubjectEnrollmentsAsync(Guid subjectId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Where(se => se.SubjectId == subjectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(Guid sectionId)
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

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSubjectEnrollmentsAsync(Guid subjectId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Student)
                .ThenInclude(s => s.User)
            .Include(se => se.Section)
            .Where(se => se.SubjectId == subjectId && se.IsActive)
            .ToListAsync();
    }

    public async Task<StudentEnrollment?> GetEnrollmentAsync(Guid studentId, Guid sectionId, Guid subjectId)
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

    public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        return await _context.StudentEnrollments
        .AsNoTracking()
        .AnyAsync(se => se.StudentId == studentId &&
        se.SectionId == sectionId &&
        se.SubjectId == subjectId &&
                           se.IsActive);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveEnrollmentsAsync(Guid studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Section>> GetStudentSectionsAsync(Guid studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Section)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .Select(se => se.Section)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<Subject>> GetStudentSubjectsAsync(Guid studentId)
    {
        return await _context.StudentEnrollments
            .AsNoTracking()
            .Include(se => se.Subject)
            .Where(se => se.StudentId == studentId && se.IsActive)
            .Select(se => se.Subject)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> DeactivateEnrollmentAsync(Guid enrollmentId)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(enrollmentId);
        if (enrollment == null) return false;

        enrollment.IsActive = false;
        enrollment.DroppedAt = DateTime.UtcNow;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateEnrollmentAsync(Guid enrollmentId)
    {
        var enrollment = await _context.StudentEnrollments.FindAsync(enrollmentId);
        if (enrollment == null) return false;

        enrollment.IsActive = true;
        enrollment.DroppedAt = null;
        enrollment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets enrollments for a specific student.
    /// Used for finding active sessions in fingerprint attendance.
    /// </summary>
    public async Task<IEnumerable<StudentEnrollment>> GetByStudentIdAsync(Guid studentId)
    {
        return await GetStudentEnrollmentsAsync(studentId);
    }

    /// <summary>
    /// Gets a specific enrollment for a student in a section-subject combination.
    /// Used for verifying enrollment in fingerprint attendance.
    /// </summary>
    public async Task<StudentEnrollment?> GetByStudentSectionSubjectAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        return await GetEnrollmentAsync(studentId, sectionId, subjectId);
    }
}
