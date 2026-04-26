using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IStudentEnrollmentService
{
    Task<StudentEnrollment> EnrollStudentAsync(Guid studentId, Guid sectionId, Guid subjectId, string enrollmentType = "Irregular", string? academicYear = null, string? semester = null);
    Task<StudentEnrollment> EnrollStudentAsync(CreateStudentEnrollment request);
    Task<Student> GetStudentByIdAsync(Guid studentId);
    Task<bool> UnenrollStudentAsync(Guid studentId, Guid sectionId, Guid subjectId);
    Task<bool> DropStudentFromSubjectAsync(Guid enrollmentId);
    Task<bool> ReenrollStudentAsync(Guid enrollmentId);
    Task<StudentEnrollment> GetEnrollmentByUuidAsync(Guid enrollmentUuid);
    Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(Guid studentId);
    Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsByStudentUuidAsync(Guid studentUuid);
    Task<IEnumerable<StudentEnrollment>> GetActiveStudentEnrollmentsAsync(Guid studentId);
    Task<IEnumerable<Section>> GetStudentSectionsAsync(Guid studentId);
    Task<IEnumerable<Subject>> GetStudentSubjectsAsync(Guid studentId);
    Task<bool> IsStudentEnrolledInSectionSubjectAsync(Guid studentId, Guid sectionId, Guid subjectId);
    Task<IEnumerable<Student>> GetStudentsInSectionAsync(Guid sectionId);
    Task<IEnumerable<Student>> GetStudentsInSubjectAsync(Guid subjectId);
    Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(Guid sectionId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(Guid sectionId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsBySectionUuidAsync(Guid sectionUuid);
    Task<StudentEnrollment?> GetSpecificEnrollmentAsync(Guid studentId, Guid sectionId, Guid subjectId);
    Task<bool> IsStudentEnrolledInSectionSubjectByUuidAsync(Guid studentUuid, Guid sectionUuid, Guid subjectUuid);
}
