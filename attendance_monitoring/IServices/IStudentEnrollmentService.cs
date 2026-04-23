using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IStudentEnrollmentService
{
    Task<StudentEnrollment> EnrollStudentAsync(int studentId, int sectionId, int subjectId, string enrollmentType = "Irregular", string? academicYear = null, string? semester = null);
    Task<StudentEnrollment> EnrollStudentAsync(CreateStudentEnrollment request);
    Task<bool> UnenrollStudentAsync(int studentId, int sectionId, int subjectId);
    Task<bool> DropStudentFromSubjectAsync(int enrollmentId);
    Task<bool> DropStudentFromSubjectAsync(Guid enrollmentUuid);
    Task<bool> ReenrollStudentAsync(int enrollmentId);
    Task<bool> ReenrollStudentAsync(Guid enrollmentUuid);
    Task<StudentEnrollment> GetEnrollmentByUuidAsync(Guid enrollmentUuid);
    Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(int studentId);
    Task<IEnumerable<StudentEnrollment>> GetActiveStudentEnrollmentsAsync(int studentId);
    Task<IEnumerable<Section>> GetStudentSectionsAsync(int studentId);
    Task<IEnumerable<Subject>> GetStudentSubjectsAsync(int studentId);
    Task<bool> IsStudentEnrolledInSectionSubjectAsync(int studentId, int sectionId, int subjectId);
    Task<IEnumerable<Student>> GetStudentsInSectionAsync(int sectionId);
    Task<IEnumerable<Student>> GetStudentsInSubjectAsync(int subjectId);
    Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(int sectionId);
    Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(int sectionId);
    Task<StudentEnrollment?> GetSpecificEnrollmentAsync(int studentId, int sectionId, int subjectId);
}
