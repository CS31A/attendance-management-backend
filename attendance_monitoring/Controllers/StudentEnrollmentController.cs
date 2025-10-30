using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;

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
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<StudentEnrollmentResponseDto>> EnrollStudent([FromBody] CreateStudentEnrollment request)
    {
        try
        {
            _logger.LogInformation("Enrolling student {StudentId} in section {SectionId} for subject {SubjectId}",
                request.StudentId, request.SectionId, request.SubjectId);

            var enrollment = await _enrollmentService.EnrollStudentAsync(
                request.StudentId,
                request.SectionId,
                request.SubjectId,
                request.EnrollmentType,
                request.AcademicYear,
                request.Semester);

            var response = new StudentEnrollmentResponseDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                StudentFirstname = enrollment.Student?.Firstname,
                StudentLastname = enrollment.Student?.Lastname,
                StudentEmail = enrollment.Student?.User?.Email,
                SectionId = enrollment.SectionId,
                SectionName = enrollment.Section?.Name,
                SubjectId = enrollment.SubjectId,
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

            _logger.LogInformation("Successfully enrolled student {StudentId}", request.StudentId);
            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            _logger.LogWarning(ex, "Entity not found while enrolling student {StudentId}", request.StudentId);
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
        // No generic catch - let global exception handler handle unexpected errors
    }

    /// <summary>
    /// Get all enrollments for a specific student
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollments(int studentId)
    {
        _logger.LogInformation("Retrieving enrollments for student {StudentId}", studentId);

        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);

        var response = new StudentSectionsResponseDto
        {
            StudentId = studentId,
            Enrollments = enrollments.Select(e => new EnrollmentSummaryDto
            {
                EnrollmentId = e.Id,
                SectionId = e.SectionId,
                SectionName = e.Section?.Name,
                SubjectId = e.SubjectId,
                SubjectName = e.Subject?.Name,
                SubjectCode = e.Subject?.Code,
                EnrollmentType = e.EnrollmentType,
                IsActive = e.IsActive,
                EnrolledAt = e.EnrolledAt
            }).ToList()
        };

        _logger.LogInformation("Successfully retrieved {Count} enrollments for student {StudentId}",
            response.Enrollments.Count, studentId);

        return Ok(response);
        // No try-catch - let global exception handler handle any errors
    }

    /// <summary>
    /// Get all active students enrolled in a specific section
    /// </summary>
    [HttpGet("section/{sectionId}/students")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudents(int sectionId)
    {
        _logger.LogInformation("Retrieving active students for section {SectionId}", sectionId);

        // Get only active enrollments from database (database-level filtering, no in-memory filtering)
        var enrollments = await _enrollmentService.GetActiveSectionEnrollmentsAsync(sectionId);

        var response = enrollments.Select(e => new StudentEnrollmentResponseDto
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentFirstname = e.Student?.Firstname,
            StudentLastname = e.Student?.Lastname,
            StudentEmail = e.Student?.User?.Email,
            SectionId = e.SectionId,
            SectionName = e.Section?.Name,
            SubjectId = e.SubjectId,
            SubjectName = e.Subject?.Name,
            SubjectCode = e.Subject?.Code,
            IsActive = e.IsActive,
            EnrollmentType = e.EnrollmentType,
            AcademicYear = e.AcademicYear,
            Semester = e.Semester,
            EnrolledAt = e.EnrolledAt,
            DroppedAt = e.DroppedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });

        _logger.LogInformation("Successfully retrieved students for section {SectionId}", sectionId);
        return Ok(response);
        // No try-catch - let global exception handler handle any errors
    }

    /// <summary>
    /// Drop a student from a specific enrollment (deactivate)
    /// </summary>
    [HttpPatch("{enrollmentId}/drop")]
    [Authorize(Roles = "Admin,Instructor")]
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
        // No generic catch - let global exception handler handle unexpected errors
    }

    /// <summary>
    /// Re-enroll a student (reactivate enrollment)
    /// </summary>
    [HttpPatch("{enrollmentId}/reenroll")]
    [Authorize(Roles = "Admin,Instructor")]
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
        // No generic catch - let global exception handler handle unexpected errors
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
        // No try-catch - let global exception handler handle any errors
    }
}