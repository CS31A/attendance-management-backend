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
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Entity not found while enrolling student {StudentId}", request.StudentId);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityAlreadyExistsException<string> ex)
        {
            _logger.LogWarning(ex, "Duplicate enrollment attempt for student {StudentId}", request.StudentId);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityAlreadyExistsException<Guid> ex)
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
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollments(Guid studentId)
    {
        _logger.LogInformation("Retrieving enrollments for student {StudentId}", studentId);

        Student student;
        try
        {
            student = await _enrollmentService.GetStudentByIdAsync(studentId).ConfigureAwait(false);
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Student {StudentId} not found while retrieving enrollments", studentId);
            return NotFound(new { message = ex.Message });
        }

        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        var response = MapStudentSectionsResponse(student.Id, enrollments);

        _logger.LogInformation("Successfully retrieved {Count} enrollments for student {StudentId}",
            response.Enrollments.Count, studentId);

        return Ok(response);
    }

    [HttpGet("student/{id:guid}")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollmentsByUuid([FromRoute(Name = "id")] Guid studentId)
    {
        _logger.LogInformation("Retrieving enrollments for student ID {StudentId}", studentId);

        var enrollments = await _enrollmentService.GetStudentEnrollmentsByStudentUuidAsync(studentId);
        var response = MapStudentSectionsResponse(studentId, enrollments);

        _logger.LogInformation("Successfully retrieved {Count} enrollments for student ID {StudentId}",
            response.Enrollments.Count, studentId);

        return Ok(response);
    }

    /// <summary>
    /// Get all active students enrolled in a specific section
    /// </summary>
    [HttpGet("section/{sectionId:int}/students")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudents(Guid sectionId)
    {
        _logger.LogInformation("Retrieving active students for section {SectionId}", sectionId);

        var enrollments = await _enrollmentService.GetActiveSectionEnrollmentsAsync(sectionId);
        var response = enrollments.Select(MapEnrollmentResponse);

        _logger.LogInformation("Successfully retrieved students for section {SectionId}", sectionId);
        return Ok(response);
    }

    [HttpGet("section/{id:guid}/students")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudentsByUuid([FromRoute(Name = "id")] Guid sectionId)
    {
        _logger.LogInformation("Retrieving active students for section ID {SectionId}", sectionId);

        var enrollments = await _enrollmentService.GetActiveSectionEnrollmentsBySectionUuidAsync(sectionId);
        var response = enrollments.Select(MapEnrollmentResponse);

        _logger.LogInformation("Successfully retrieved students for section ID {SectionId}", sectionId);
        return Ok(response);
    }

    /// <summary>
    /// Drop a student from a specific enrollment (deactivate)
    /// </summary>
    [HttpPatch("{enrollmentId:int}/drop")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DropStudent(Guid enrollmentId)
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
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment {EnrollmentId} not found", enrollmentId);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/drop")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DropStudentByUuid([FromRoute(Name = "id")] Guid id)
    {
        try
        {
            _logger.LogInformation("Dropping student from enrollment UUID {EnrollmentUuid}", id);
            var success = await _enrollmentService.DropStudentFromSubjectAsync(id);

            if (!success)
            {
                _logger.LogWarning("Enrollment UUID {EnrollmentUuid} not found", id);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully dropped student from enrollment UUID {EnrollmentUuid}", id);
            return Ok(new { message = "Student successfully dropped from enrollment" });
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment UUID {EnrollmentUuid} not found", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Re-enroll a student (reactivate enrollment)
    /// </summary>
    [HttpPatch("{enrollmentId:int}/reenroll")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReenrollStudent(Guid enrollmentId)
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
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment {EnrollmentId} not found", enrollmentId);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/reenroll")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReenrollStudentByUuid([FromRoute(Name = "id")] Guid id)
    {
        try
        {
            _logger.LogInformation("Re-enrolling student for enrollment UUID {EnrollmentUuid}", id);
            var success = await _enrollmentService.ReenrollStudentAsync(id);

            if (!success)
            {
                _logger.LogWarning("Enrollment UUID {EnrollmentUuid} not found", id);
                return NotFound(new { message = "Enrollment not found" });
            }

            _logger.LogInformation("Successfully re-enrolled student for enrollment UUID {EnrollmentUuid}", id);
            return Ok(new { message = "Student successfully re-enrolled" });
        }
        catch (EntityNotFoundException<Guid> ex)
        {
            _logger.LogWarning(ex, "Enrollment UUID {EnrollmentUuid} not found", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if a student is enrolled in a specific section-subject combination
    /// </summary>
    [HttpGet("check/legacy")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<bool>> CheckEnrollment([FromQuery] Guid studentId, [FromQuery] Guid sectionId, [FromQuery] Guid subjectId)
    {
        _logger.LogInformation("Checking enrollment for student {StudentId}, section {SectionId}, subject {SubjectId}",
            studentId, sectionId, subjectId);

        var isEnrolled = await _enrollmentService.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId);

        return Ok(new { isEnrolled });
    }

    [HttpGet("check")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<bool>> CheckEnrollmentByUuid([FromQuery] Guid studentId, [FromQuery] Guid sectionId, [FromQuery] Guid subjectId)
    {
        _logger.LogInformation("Checking enrollment for student ID {StudentId}, section ID {SectionId}, subject ID {SubjectId}",
            studentId, sectionId, subjectId);

        var isEnrolled = await _enrollmentService.IsStudentEnrolledInSectionSubjectByUuidAsync(studentId, sectionId, subjectId);

        return Ok(new { isEnrolled });
    }

    private static StudentEnrollmentResponseDto MapEnrollmentResponse(StudentEnrollment enrollment)
    {
        return new StudentEnrollmentResponseDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.Student?.Id ?? Guid.Empty,
            StudentFirstname = enrollment.Student?.Firstname,
            StudentLastname = enrollment.Student?.Lastname,
            StudentEmail = enrollment.Student?.User?.Email,
            SectionId = enrollment.Section?.Id ?? Guid.Empty,
            SectionName = enrollment.Section?.Name,
            SubjectId = enrollment.Subject?.Id ?? Guid.Empty,
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

    private static StudentSectionsResponseDto MapStudentSectionsResponse(Guid studentId, IEnumerable<StudentEnrollment> enrollments)
    {
        var enrollmentList = enrollments.ToList();
        var student = enrollmentList.FirstOrDefault()?.Student;

        return new StudentSectionsResponseDto
        {
            StudentId = studentId,
            StudentFirstname = student?.Firstname,
            StudentLastname = student?.Lastname,
            IsRegular = student?.IsRegular ?? false,
            Enrollments = enrollmentList.Select(e => new EnrollmentSummaryDto
            {
                EnrollmentId = e.Id,
                SectionId = e.Section?.Id ?? Guid.Empty,
                SectionName = e.Section?.Name,
                SubjectId = e.Subject?.Id ?? Guid.Empty,
                SubjectName = e.Subject?.Name,
                SubjectCode = e.Subject?.Code,
                EnrollmentType = e.EnrollmentType,
                IsActive = e.IsActive,
                EnrolledAt = e.EnrolledAt
            }).ToList()
        };
    }
}
