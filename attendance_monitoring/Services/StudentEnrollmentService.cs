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

    public async Task<StudentEnrollment> EnrollStudentAsync(int studentId, int sectionId, int subjectId, string enrollmentType = "Irregular", string? academicYear = null, string? semester = null)
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
                throw new EntityNotFoundException<int>("Student", studentId);

            // Validate section exists
            var section = await _sectionRepository.GetSectionByIdAsync(sectionId);
            if (section == null)
                throw new EntityNotFoundException<int>("Section", sectionId);

            // Validate subject exists
            var subject = await _subjectRepository.GetSubjectByIdAsync(subjectId);
            if (subject == null)
                throw new EntityNotFoundException<int>("Subject", subjectId);

            // Check if this is the student's primary section (regular enrollment)
            if (student.SectionId == sectionId)
            {
                throw new EntityAlreadyExistsException<string>("Enrollment", "Combination", "Student is already in this section as their primary section. Additional enrollment not needed.");
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
        catch (Exception ex) when (ex is not ValidationException and not EntityNotFoundException<int> and not EntityAlreadyExistsException<string>)
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
        var studentId = await ResolveStudentIdAsync(request.StudentId, request.StudentUuid).ConfigureAwait(false);
        var sectionId = await ResolveSectionIdAsync(request.SectionId, request.SectionUuid).ConfigureAwait(false);
        var subjectId = await ResolveSubjectIdAsync(request.SubjectId, request.SubjectUuid).ConfigureAwait(false);

        return await EnrollStudentAsync(
            studentId,
            sectionId,
            subjectId,
            request.EnrollmentType,
            request.AcademicYear,
            request.Semester).ConfigureAwait(false);
    }

    public async Task<bool> UnenrollStudentAsync(int studentId, int sectionId, int subjectId)
    {
        var enrollment = await _enrollmentRepository.GetEnrollmentAsync(studentId, sectionId, subjectId);
        if (enrollment == null)
            return false;

        return await _enrollmentRepository.DeleteAsync(enrollment.Id);
    }

    public async Task<bool> DropStudentFromSubjectAsync(int enrollmentId)
    {
        return await _enrollmentRepository.DeactivateEnrollmentAsync(enrollmentId);
    }

    public async Task<bool> DropStudentFromSubjectAsync(Guid enrollmentUuid)
    {
        var enrollment = await GetEnrollmentByUuidAsync(enrollmentUuid).ConfigureAwait(false);
        return await DropStudentFromSubjectAsync(enrollment.Id).ConfigureAwait(false);
    }

    public async Task<bool> ReenrollStudentAsync(int enrollmentId)
    {
        return await _enrollmentRepository.ReactivateEnrollmentAsync(enrollmentId);
    }

    public async Task<bool> ReenrollStudentAsync(Guid enrollmentUuid)
    {
        var enrollment = await GetEnrollmentByUuidAsync(enrollmentUuid).ConfigureAwait(false);
        return await ReenrollStudentAsync(enrollment.Id).ConfigureAwait(false);
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

    public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(int studentId)
    {
        return await _enrollmentRepository.GetStudentEnrollmentsAsync(studentId);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveStudentEnrollmentsAsync(int studentId)
    {
        return await _enrollmentRepository.GetActiveEnrollmentsAsync(studentId);
    }

    public async Task<IEnumerable<Section>> GetStudentSectionsAsync(int studentId)
    {
        return await _enrollmentRepository.GetStudentSectionsAsync(studentId);
    }

    public async Task<IEnumerable<Subject>> GetStudentSubjectsAsync(int studentId)
    {
        return await _enrollmentRepository.GetStudentSubjectsAsync(studentId);
    }

    public async Task<bool> IsStudentEnrolledInSectionSubjectAsync(int studentId, int sectionId, int subjectId)
    {
        return await _enrollmentRepository.IsStudentEnrolledAsync(studentId, sectionId, subjectId);
    }

    public async Task<IEnumerable<Student>> GetStudentsInSectionAsync(int sectionId)
    {
        // Use database-level filtering for better performance
        var enrollments = await _enrollmentRepository.GetActiveSectionEnrollmentsAsync(sectionId);
        return enrollments.Select(e => e.Student).Distinct();
    }

    public async Task<IEnumerable<Student>> GetStudentsInSubjectAsync(int subjectId)
    {
        // Use database-level filtering for better performance
        var enrollments = await _enrollmentRepository.GetActiveSubjectEnrollmentsAsync(subjectId);
        return enrollments.Select(e => e.Student).Distinct();
    }

    public async Task<IEnumerable<StudentEnrollment>> GetSectionEnrollmentsAsync(int sectionId)
    {
        return await _enrollmentRepository.GetSectionEnrollmentsAsync(sectionId);
    }

    public async Task<IEnumerable<StudentEnrollment>> GetActiveSectionEnrollmentsAsync(int sectionId)
    {
        return await _enrollmentRepository.GetActiveSectionEnrollmentsAsync(sectionId);
    }

    public async Task<StudentEnrollment?> GetSpecificEnrollmentAsync(int studentId, int sectionId, int subjectId)
    {
        return await _enrollmentRepository.GetEnrollmentAsync(studentId, sectionId, subjectId);
    }

    private async Task<int> ResolveStudentIdAsync(int? studentId, Guid? studentUuid)
    {
        return await EntityIdResolutionHelper.ResolveEntityIdAsync(
            studentId,
            studentUuid,
            "Student",
            async id => (await _mockableStudentByIdAsync(id).ConfigureAwait(false))?.Id,
            async uuid => (await _studentRepository.GetStudentByUuidAsync(uuid).ConfigureAwait(false))?.Id).ConfigureAwait(false);
    }

    private async Task<int> ResolveSectionIdAsync(int? sectionId, Guid? sectionUuid)
    {
        return await EntityIdResolutionHelper.ResolveEntityIdAsync(
            sectionId,
            sectionUuid,
            "Section",
            async id => (await _sectionRepository.GetSectionByIdAsync(id).ConfigureAwait(false))?.Id,
            async uuid => (await _sectionRepository.GetSectionByUuidAsync(uuid).ConfigureAwait(false))?.Id).ConfigureAwait(false);
    }

    private async Task<int> ResolveSubjectIdAsync(int? subjectId, Guid? subjectUuid)
    {
        return await EntityIdResolutionHelper.ResolveEntityIdAsync(
            subjectId,
            subjectUuid,
            "Subject",
            async id => (await _subjectRepository.GetSubjectByIdAsync(id).ConfigureAwait(false))?.Id,
            async uuid => (await _subjectRepository.GetSubjectByUuidAsync(uuid).ConfigureAwait(false))?.Id).ConfigureAwait(false);
    }

    private async Task<Student?> _mockableStudentByIdAsync(int studentId)
    {
        return await _studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
    }
}
