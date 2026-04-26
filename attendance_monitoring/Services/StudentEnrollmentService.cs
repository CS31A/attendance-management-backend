using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.Services;

public class StudentEnrollmentService : IStudentEnrollmentService
{
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<StudentEnrollmentService> _logger;

    public StudentEnrollmentService(
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ISectionRepository sectionRepository,
        ISubjectRepository subjectRepository,
        ILogger<StudentEnrollmentService> logger)
    {
        _enrollmentRepository = enrollmentRepository ?? throw new ArgumentNullException(nameof(enrollmentRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _subjectRepository = subjectRepository ?? throw new ArgumentNullException(nameof(subjectRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StudentEnrollment> EnrollStudentAsync(Guid studentId, Guid sectionId, Guid subjectId, string enrollmentType = "Irregular", string? academicYear = null, string? semester = null)
    {
        try
        {
            if (!EnrollmentTypeConstants.IsValid(enrollmentType))
            {
                throw new ValidationException($"Enrollment type must be one of: {string.Join(", ", EnrollmentTypeConstants.All)}");
            }

            var normalizedEnrollmentType = EnrollmentTypeConstants.Normalize(enrollmentType);

            // Validate student exists
            var student = await _studentRepository.GetStudentByIdAsync(studentId);
            if (student == null || student.IsDeleted)
                throw new EntityNotFoundException<Guid>("Student", studentId);

            // Validate section exists
            var section = await _sectionRepository.GetSectionByIdAsync(sectionId);
            if (section == null)
                throw new EntityNotFoundException<Guid>("Section", sectionId);

            // Validate subject exists
            var subject = await _subjectRepository.GetSubjectByIdAsync(subjectId);
            if (subject == null)
                throw new EntityNotFoundException<Guid>("Subject", subjectId);

            // Check if this is the student's primary section (regular enrollment)
            if (student.SectionId == sectionId)
            {
                throw new EntityAlreadyExistsException<string>("Enrollment", "Combination", "Student is already in this section as their primary section. Additional enrollment not needed.");
            }

            // Validate enrollment type for cross-section enrollments
            // Cross-section enrollments must be Irregular or Retake, not Regular
            if (student.SectionId != sectionId && normalizedEnrollmentType == EnrollmentTypeConstants.Regular)
            {
                throw new ValidationException($"Cross-section enrollments cannot be 'Regular'. Student's primary section is {student.SectionId}, but enrollment section is {sectionId}. Use 'Irregular' or 'Retake' instead.");
            }

            // Check if student is already enrolled in this specific combination
            var existingEnrollment = await _enrollmentRepository.GetEnrollmentAsync(studentId, sectionId, subjectId);
            if (existingEnrollment != null)
            {
                if (existingEnrollment.IsActive)
                    throw new EntityAlreadyExistsException<string>("Enrollment", "Combination", "Student is already enrolled in this section-subject combination");

                // Reactivate if exists but inactive
                await _enrollmentRepository.ReactivateEnrollmentAsync(existingEnrollment.Id);

                // Reload the enrollment from database to get updated values (IsActive, UpdatedAt, etc.)
                var reactivatedEnrollment = await _enrollmentRepository.GetByIdAsync(existingEnrollment.Id);
                return reactivatedEnrollment ?? existingEnrollment; // Fallback to existing if reload fails
            }

            // Create new additional enrollment (for irregular students)
            var enrollment = new StudentEnrollment
            {
                StudentId = studentId,
                SectionId = sectionId,
                SubjectId = subjectId,
                EnrollmentType = normalizedEnrollmentType,
                AcademicYear = academicYear,
                Semester = semester,
                IsActive = true
            };

            return await _enrollmentRepository.CreateAsync(enrollment);
        }
        catch (Exception ex) when (ex is not ValidationException and not EntityNotFoundException<Guid> and not EntityAlreadyExistsException<string>)
        {
            _logger.LogError(ex,
                "Enrollment operation failed for student {StudentId}, section {SectionId}, subject {SubjectId}",
                studentId,
                sectionId,
                subjectId);
            throw;
        }
    }

    public async Task<StudentEnrollment> EnrollStudentAsync(CreateStudentEnrollment request)
    {
        var studentId = await ResolveStudentIdAsync(request.StudentId).ConfigureAwait(false);
        var sectionId = await ResolveSectionIdAsync(request.SectionId).ConfigureAwait(false);
        var subjectId = await ResolveSubjectIdAsync(request.SubjectId).ConfigureAwait(false);

        return await EnrollStudentAsync(
            studentId,
            sectionId,
            subjectId,
            request.EnrollmentType,
            request.AcademicYear,
            request.Semester).ConfigureAwait(false);
    }

    public async Task<Student> GetStudentByIdAsync(Guid studentId)
    {
        var student = await _studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
        if (student == null || student.IsDeleted)
        {
            throw new EntityNotFoundException<Guid>("Student", studentId);
        }

        return student;
    }

    public async Task<bool> UnenrollStudentAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        var enrollment = await _enrollmentRepository.GetEnrollmentAsync(studentId, sectionId, subjectId);
        if (enrollment == null)
            return false;

        return await _enrollmentRepository.DeleteAsync(enrollment.Id);
    }

    public async Task<bool> DropStudentFromSubjectAsync(Guid enrollmentId)
    {
        return await _enrollmentRepository.DeactivateEnrollmentAsync(enrollmentId);
    }

    public async Task<bool> ReenrollStudentAsync(Guid enrollmentId)
    {
        return await _enrollmentRepository.ReactivateEnrollmentAsync(enrollmentId);
    }

    public async Task<StudentEnrollment> GetEnrollmentByUuidAsync(Guid enrollmentUuid)
    {
        var enrollment = await _enrollmentRepository.GetByUuidAsync(enrollmentUuid).ConfigureAwait(false);
        if (enrollment == null)
        {
            throw new EntityNotFoundException<Guid>("Enrollment", enrollmentUuid);
        }

        return enrollment;
    }

    public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(Guid studentId)
    {
        return await _enrollmentRepository.GetStudentEnrollmentsAsync(studentId);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsByStudentUuidAsync(Guid studentUuid)
    {
        var studentId = await ResolveStudentIdAsync(studentUuid).ConfigureAwait(false);
        return await GetStudentEnrollmentsAsync(studentId).ConfigureAwait(false);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveStudentEnrollmentsAsync(Guid studentId)
    {
        return await _enrollmentRepository.GetActiveEnrollmentsAsync(studentId);
    }

    public async Task<IEnumerable<Section>> GetStudentSectionsAsync(Guid studentId)
    {
        return await _enrollmentRepository.GetStudentSectionsAsync(studentId);
    }

    public async Task<IEnumerable<Subject>> GetStudentSubjectsAsync(Guid studentId)
    {
        return await _enrollmentRepository.GetStudentSubjectsAsync(studentId);
    }

    public async Task<bool> IsStudentEnrolledInSectionSubjectAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        return await _enrollmentRepository.IsStudentEnrolledAsync(studentId, sectionId, subjectId);
    }

    public async Task<bool> IsStudentEnrolledInSectionSubjectByUuidAsync(Guid studentUuid, Guid sectionUuid, Guid subjectUuid)
    {
        var studentId = await ResolveStudentIdAsync(studentUuid).ConfigureAwait(false);
        var sectionId = await ResolveSectionIdAsync(sectionUuid).ConfigureAwait(false);
        var subjectId = await ResolveSubjectIdAsync(subjectUuid).ConfigureAwait(false);
        return await IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Student>> GetStudentsInSectionAsync(Guid sectionId)
    {
        // Use database-level filtering for better performance
        var enrollments = await _enrollmentRepository.GetActiveSectionEnrollmentsAsync(sectionId);
        return enrollments.Select(e => e.Student).Distinct();
    }

    public async Task<IEnumerable<Student>> GetStudentsInSubjectAsync(Guid subjectId)
    {
        // Use database-level filtering for better performance
        var enrollments = await _enrollmentRepository.GetActiveSubjectEnrollmentsAsync(subjectId);
        return enrollments.Select(e => e.Student).Distinct();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(Guid sectionId)
    {
        return await _enrollmentRepository.GetSectionEnrollmentsAsync(sectionId);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(Guid sectionId)
    {
        return await _enrollmentRepository.GetActiveSectionEnrollmentsAsync(sectionId);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsBySectionUuidAsync(Guid sectionUuid)
    {
        var sectionId = await ResolveSectionIdAsync(sectionUuid).ConfigureAwait(false);
        return await GetActiveSectionEnrollmentsAsync(sectionId).ConfigureAwait(false);
    }

    public async Task<StudentEnrollment?> GetSpecificEnrollmentAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        return await _enrollmentRepository.GetEnrollmentAsync(studentId, sectionId, subjectId);
    }

    private async Task<Guid> ResolveStudentIdAsync(Guid studentUuid)
    {
        var student = await _studentRepository.GetStudentByIdAsync(studentUuid).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<Guid>("Student", studentUuid);
        }

        return student.Id;
    }

    private async Task<Guid> ResolveSectionIdAsync(Guid sectionUuid)
    {
        var section = await _sectionRepository.GetSectionByIdAsync(sectionUuid).ConfigureAwait(false);
        if (section == null)
        {
            throw new EntityNotFoundException<Guid>("Section", sectionUuid);
        }

        return section.Id;
    }

    private async Task<Guid> ResolveSubjectIdAsync(Guid subjectUuid)
    {
        var subject = await _subjectRepository.GetSubjectByIdAsync(subjectUuid).ConfigureAwait(false);
        if (subject == null)
        {
            throw new EntityNotFoundException<Guid>("Subject", subjectUuid);
        }

        return subject.Id;
    }

    private async Task<Student?> _mockableStudentByIdAsync(Guid studentId)
    {
        return await _studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
    }
}
