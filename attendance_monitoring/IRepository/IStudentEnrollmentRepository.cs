using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

public interface IStudentEnrollmentRepository
{
    Task<IEnumerable<StudentEnrollment>> GetAllAsync();
    Task<StudentEnrollment?> GetByIdAsync(Guid id);
    Task<StudentEnrollment?> GetByIdTrackedAsync(Guid id);
    Task<StudentEnrollment?> GetByUuidAsync(Guid id);
    Task<StudentEnrollment?> GetByUuidTrackedAsync(Guid id);
    Task<StudentEnrollment> CreateAsync(StudentEnrollment enrollment);
    Task<StudentEnrollment> UpdateAsync(StudentEnrollment enrollment);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(Guid studentId);
    Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(Guid sectionId);
    Task<IEnumerable<StudentEnrollment>> GetSubjectEnrollmentsAsync(Guid subjectId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(Guid sectionId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSubjectEnrollmentsAsync(Guid subjectId);
    Task<StudentEnrollment?> GetEnrollmentAsync(Guid studentId, Guid sectionId, Guid subjectId);
    Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid sectionId, Guid subjectId);
    Task<IEnumerable<StudentEnrollment>> GetActiveEnrollmentsAsync(Guid studentId);
    Task<IEnumerable<Section>> GetStudentSectionsAsync(Guid studentId);
    Task<IEnumerable<Subject>> GetStudentSubjectsAsync(Guid studentId);
    Task<bool> DeactivateEnrollmentAsync(Guid enrollmentId);
    Task<bool> ReactivateEnrollmentAsync(Guid enrollmentId);

    // New methods for fingerprint attendance
    /// <summary>
    /// Gets all enrollments for a student.
    /// </summary>
    Task<IEnumerable<StudentEnrollment>> GetByStudentIdAsync(Guid studentId);

    /// <summary>
    /// Gets a specific enrollment for a student in a section-subject combination.
    /// </summary>
    Task<StudentEnrollment?> GetByStudentSectionSubjectAsync(Guid studentId, Guid sectionId, Guid subjectId);
}
