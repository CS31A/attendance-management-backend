using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

public interface IStudentEnrollmentRepository
{
    Task<IEnumerable<StudentEnrollment>> GetAllAsync();
    Task<StudentEnrollment?> GetByIdAsync(int id);
    Task<StudentEnrollment?> GetByIdTrackedAsync(int id);
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
}