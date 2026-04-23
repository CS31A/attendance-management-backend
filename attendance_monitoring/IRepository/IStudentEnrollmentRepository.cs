using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

public interface IStudentEnrollmentRepository
{
    Task<IEnumerable<StudentEnrollment>> GetAllAsync();
    Task<StudentEnrollment?> GetByIdAsync(int id);
    Task<StudentEnrollment?> GetByIdTrackedAsync(int id);
    Task<StudentEnrollment?> GetByUuidAsync(Guid uuid);
    Task<StudentEnrollment?> GetByUuidTrackedAsync(Guid uuid);
    Task<StudentEnrollment> CreateAsync(StudentEnrollment enrollment);
    Task<StudentEnrollment> UpdateAsync(StudentEnrollment enrollment);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(int studentId);
    Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(int sectionId);
    Task<IEnumerable<StudentEnrollment>> GetSubjectEnrollmentsAsync(int subjectId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(int sectionId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSubjectEnrollmentsAsync(int subjectId);
    Task<StudentEnrollment?> GetEnrollmentAsync(int studentId, int sectionId, int subjectId);
    Task<bool> IsStudentEnrolledAsync(int studentId, int sectionId, int subjectId);
    Task<IEnumerable<StudentEnrollment>> GetActiveEnrollmentsAsync(int studentId);
    Task<IEnumerable<Section>> GetStudentSectionsAsync(int studentId);
    Task<IEnumerable<Subject>> GetStudentSubjectsAsync(int studentId);
    Task<bool> DeactivateEnrollmentAsync(int enrollmentId);
    Task<bool> ReactivateEnrollmentAsync(int enrollmentId);

    // New methods for fingerprint attendance
    /// <summary>
    /// Gets all enrollments for a student.
    /// </summary>
    Task<IEnumerable<StudentEnrollment>> GetByStudentIdAsync(int studentId);

    /// <summary>
    /// Gets a specific enrollment for a student in a section-subject combination.
    /// </summary>
    Task<StudentEnrollment?> GetByStudentSectionSubjectAsync(int studentId, int sectionId, int subjectId);
}
