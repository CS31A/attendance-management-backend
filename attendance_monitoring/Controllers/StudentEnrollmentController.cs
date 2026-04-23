using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentEnrollmentController : ControllerBase
{
    private readonly IStudentEnrollmentService _enrollmentService;
    private readonly ILogger<StudentEnrollmentController> _logger;

    public StudentEnrollmentController(
        IStudentEnrollmentService enrollmentService,
        ILogger<StudentEnrollmentController> logger)
    {
        _enrollmentService = enrollmentService;
        _logger = logger;
    }

    /// <summary>
    /// Enroll a student in an additional section-subject combination (for irregular students)
    /// </summary>
    [HttpPost("enroll")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StudentEnrollmentResponseDto>> EnrollStudent([FromBody] CreateStudentEnrollment request)
    {
        try
        {
            _logger.LogInformation("Enrolling student {StudentId} in section {SectionId} for subject {SubjectId}",
                request.StudentId, request.SectionId, request.SubjectId);

            var enrollment = await _enrollmentService.EnrollStudentAsync(request);
            var response = MapEnrollmentResponse(enrollment);

            _logger.LogInformation("Successfully enrolled student {StudentId}", request.StudentId);
            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            _logger.LogWarning(ex, "Entity not found while enrolling student {StudentId}", request.StudentId);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Entity UUID not found while enrolling student {StudentUuid}", request.StudentUuid);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityAlreadyExistsException<string> ex)
        {
            _logger.LogWarning(ex, "Duplicate enrollment attempt for student {StudentId}", request.StudentId);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityAlreadyExistsException<int> ex)
        {
            _logger.LogWarning(ex, "Duplicate enrollment attempt for student {StudentId}", request.StudentId);
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid enrollment request for student {StudentId}", request.StudentId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all enrollments for a specific student
    /// </summary>
    [HttpGet("student/{studentId:int}")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollments(int studentId)
    {
        _logger.LogInformation("Retrieving enrollments for student {StudentId}", studentId);

        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        var response = MapStudentSectionsResponse(studentId, enrollments);

        _logger.LogInformation("Successfully retrieved {Count} enrollments for student {StudentId}",
            response.Enrollments.Count, studentId);

        return Ok(response);
    }

    [HttpGet("student/uuid/{studentUuid:guid}")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollmentsByUuid(Guid studentUuid)
    {
        _logger.LogInformation("Retrieving enrollments for student UUID {StudentUuid}", studentUuid);

        var enrollments = await _enrollmentService.GetStudentEnrollmentsByStudentUuidAsync(studentUuid);
        var response = MapStudentSectionsResponse(enrollments.FirstOrDefault()?.StudentId ?? 0, enrollments, studentUuid);

        _logger.LogInformation("Successfully retrieved {Count} enrollments for student UUID {StudentUuid}",
            response.Enrollments.Count, studentUuid);

        return Ok(response);
    }

    /// <summary>
    /// Get all active students enrolled in a specific section
    /// </summary>
    [HttpGet("section/{sectionId:int}/students")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudents(int sectionId)
    {
        _logger.LogInformation("Retrieving active students for section {SectionId}", sectionId);

        var enrollments = await _enrollmentService.GetActiveSectionEnrollmentsAsync(sectionId);
        var response = enrollments.Select(MapEnrollmentResponse);

        _logger.LogInformation("Successfully retrieved students for section {SectionId}", sectionId);
        return Ok(response);
    }

    [HttpGet("section/uuid/{sectionUuid:guid}/students")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudentsByUuid(Guid sectionUuid)
    {
        _logger.LogInformation("Retrieving active students for section UUID {SectionUuid}", sectionUuid);

        var enrollments = await _enrollmentService.GetActiveSectionEnrollmentsBySectionUuidAsync(sectionUuid);
        var response = enrollments.Select(MapEnrollmentResponse);

        _logger.LogInformation("Successfully retrieved students for section UUID {SectionUuid}", sectionUuid);
        return Ok(response);
    }

    /// <summary>
    /// Drop a student from a specific enrollment (deactivate)
    /// </summary>
    [HttpPatch("{enrollmentId:int}/drop")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DropStudent(int enrollmentId)
    {
        try
        {
            _logger.LogInformation("Dropping student from enrollment {EnrollmentId}", enrollmentId);
            var success = await _enrollmentService.DropStudentFromSubjectAsync(enrollmentId);

            if (!success)
            {
                _logger.LogWarning("Enrollment {EnrollmentId} not found", enrollmentId);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully dropped student from enrollment {EnrollmentId}", enrollmentId);
            return Ok(new { message = "Student successfully dropped from enrollment" });
        }
        catch (EntityNotFoundException<int> ex)
        {
            _logger.LogWarning(ex, "Enrollment {EnrollmentId} not found", enrollmentId);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("uuid/{uuid:guid}/drop")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DropStudentByUuid(Guid uuid)
    {
        try
        {
            _logger.LogInformation("Dropping student from enrollment UUID {EnrollmentUuid}", uuid);
            var success = await _enrollmentService.DropStudentFromSubjectAsync(uuid);

            if (!success)
            {
                _logger.LogWarning("Enrollment UUID {EnrollmentUuid} not found", uuid);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully dropped student from enrollment UUID {EnrollmentUuid}", uuid);
            return Ok(new { message = "Student successfully dropped from enrollment" });
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment UUID {EnrollmentUuid} not found", uuid);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Re-enroll a student (reactivate enrollment)
    /// </summary>
    [HttpPatch("{enrollmentId:int}/reenroll")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReenrollStudent(int enrollmentId)
    {
        try
        {
            _logger.LogInformation("Re-enrolling student for enrollment {EnrollmentId}", enrollmentId);
            var success = await _enrollmentService.ReenrollStudentAsync(enrollmentId);

            if (!success)
            {
                _logger.LogWarning("Enrollment {EnrollmentId} not found", enrollmentId);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully re-enrolled student for enrollment {EnrollmentId}", enrollmentId);
            return Ok(new { message = "Student successfully re-enrolled" });
        }
        catch (EntityNotFoundException<int> ex)
        {
            _logger.LogWarning(ex, "Enrollment {EnrollmentId} not found", enrollmentId);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("uuid/{uuid:guid}/reenroll")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReenrollStudentByUuid(Guid uuid)
    {
        try
        {
            _logger.LogInformation("Re-enrolling student for enrollment UUID {EnrollmentUuid}", uuid);
            var success = await _enrollmentService.ReenrollStudentAsync(uuid);

            if (!success)
            {
                _logger.LogWarning("Enrollment UUID {EnrollmentUuid} not found", uuid);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully re-enrolled student for enrollment UUID {EnrollmentUuid}", uuid);
            return Ok(new { message = "Student successfully re-enrolled" });
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment UUID {EnrollmentUuid} not found", uuid);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if a student is enrolled in a specific section-subject combination
    /// </summary>
    [HttpGet("check")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<bool>> CheckEnrollment([FromQuery] int studentId, [FromQuery] int sectionId, [FromQuery] int subjectId)
    {
        _logger.LogInformation("Checking enrollment for student {StudentId}, section {SectionId}, subject {SubjectId}",
            studentId, sectionId, subjectId);

        var isEnrolled = await _enrollmentService.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId);

        return Ok(new { isEnrolled });
    }

    [HttpGet("check/uuid")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<bool>> CheckEnrollmentByUuid([FromQuery] Guid studentUuid, [FromQuery] Guid sectionUuid, [FromQuery] Guid subjectUuid)
    {
        _logger.LogInformation("Checking enrollment for student UUID {StudentUuid}, section UUID {SectionUuid}, subject UUID {SubjectUuid}",
            studentUuid, sectionUuid, subjectUuid);

        var isEnrolled = await _enrollmentService.IsStudentEnrolledInSectionSubjectByUuidAsync(studentUuid, sectionUuid, subjectUuid);

        return Ok(new { isEnrolled });
    }

    private static StudentEnrollmentResponseDto MapEnrollmentResponse(StudentEnrollment enrollment)
    {
        return new StudentEnrollmentResponseDto
        {
            Id = enrollment.Id,
            Uuid = enrollment.Uuid,
            StudentId = enrollment.StudentId,
            StudentUuid = enrollment.Student?.Uuid,
            StudentFirstname = enrollment.Student?.Firstname,
            StudentLastname = enrollment.Student?.Lastname,
            StudentEmail = enrollment.Student?.User?.Email,
            SectionId = enrollment.SectionId,
            SectionUuid = enrollment.Section?.Uuid,
            SectionName = enrollment.Section?.Name,
            SubjectId = enrollment.SubjectId,
            SubjectUuid = enrollment.Subject?.Uuid,
            SubjectName = enrollment.Subject?.Name,
            SubjectCode = enrollment.Subject?.Code,
            IsActive = enrollment.IsActive,
            EnrollmentType = enrollment.EnrollmentType,
            AcademicYear = enrollment.AcademicYear,
            Semester = enrollment.Semester,
            EnrolledAt = enrollment.EnrolledAt,
            DroppedAt = enrollment.DroppedAt,
            CreatedAt = enrollment.CreatedAt,
            UpdatedAt = enrollment.UpdatedAt
        };
    }

    private static StudentSectionsResponseDto MapStudentSectionsResponse(int studentId, IEnumerable<StudentEnrollment> enrollments, Guid? studentUuid = null)
    {
        var enrollmentList = enrollments.ToList();
        var student = enrollmentList.FirstOrDefault()?.Student;

        return new StudentSectionsResponseDto
        {
            StudentId = studentId,
            StudentUuid = studentUuid ?? student?.Uuid,
            StudentFirstname = student?.Firstname,
            StudentLastname = student?.Lastname,
            Enrollments = enrollmentList.Select(e => new EnrollmentSummaryDto
            {
                EnrollmentId = e.Id,
                EnrollmentUuid = e.Uuid,
                SectionId = e.SectionId,
                SectionUuid = e.Section?.Uuid,
                SectionName = e.Section?.Name,
                SubjectId = e.SubjectId,
                SubjectUuid = e.Subject?.Uuid,
                SubjectName = e.Subject?.Name,
                SubjectCode = e.Subject?.Code,
                EnrollmentType = e.EnrollmentType,
                IsActive = e.IsActive,
                EnrolledAt = e.EnrolledAt
            }).ToList()
        };
    }
}
