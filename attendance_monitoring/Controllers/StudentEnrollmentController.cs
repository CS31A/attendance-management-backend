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
                StudentEmail = enrollment.Student?.Email,
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

            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (EntityAlreadyExistsException<string> ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling student {StudentId} in section {SectionId} for subject {SubjectId}",
                request.StudentId, request.SectionId, request.SubjectId);
            return StatusCode(500, new { message = "An error occurred while enrolling the student" });
        }
    }

    /// <summary>
    /// Get all enrollments for a specific student
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<StudentSectionsResponseDto>> GetStudentEnrollments(int studentId)
    {
        try
        {
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

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while retrieving student enrollments" });
        }
    }

    /// <summary>
    /// Get all students enrolled in a specific section
    /// </summary>
    [HttpGet("section/{sectionId}/students")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentResponseDto>>> GetSectionStudents(int sectionId)
    {
        try
        {
            var enrollments = await _enrollmentService.GetSectionEnrollmentsAsync(sectionId);
            
            var response = enrollments
                .Where(e => e.IsActive)
                .Select(e => new StudentEnrollmentResponseDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentFirstname = e.Student?.Firstname,
                    StudentLastname = e.Student?.Lastname,
                    StudentEmail = e.Student?.Email,
                    SectionId = e.SectionId,
                    SubjectId = e.SubjectId,
                    IsActive = e.IsActive,
                    EnrollmentType = e.EnrollmentType,
                    AcademicYear = e.AcademicYear,
                    Semester = e.Semester,
                    EnrolledAt = e.EnrolledAt,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students for section {SectionId}", sectionId);
            return StatusCode(500, new { message = "An error occurred while retrieving section students" });
        }
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
            var success = await _enrollmentService.DropStudentFromSubjectAsync(enrollmentId);
            
            if (!success)
                return NotFound(new { message = "Enrollment not found" });

            return Ok(new { message = "Student successfully dropped from enrollment" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping student from enrollment {EnrollmentId}", enrollmentId);
            return StatusCode(500, new { message = "An error occurred while dropping the student" });
        }
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
            var success = await _enrollmentService.ReenrollStudentAsync(enrollmentId);
            
            if (!success)
                return NotFound(new { message = "Enrollment not found" });

            return Ok(new { message = "Student successfully re-enrolled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-enrolling student for enrollment {EnrollmentId}", enrollmentId);
            return StatusCode(500, new { message = "An error occurred while re-enrolling the student" });
        }
    }

    /// <summary>
    /// Check if a student is enrolled in a specific section-subject combination
    /// </summary>
    [HttpGet("check")]
    [Authorize(Roles = "Admin,Instructor,Student")]
    public async Task<ActionResult<bool>> CheckEnrollment([FromQuery] int studentId, [FromQuery] int sectionId, [FromQuery] int subjectId)
    {
        try
        {
            var isEnrolled = await _enrollmentService.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId);
            return Ok(new { isEnrolled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enrollment for student {StudentId}, section {SectionId}, subject {SubjectId}",
                studentId, sectionId, subjectId);
            return StatusCode(500, new { message = "An error occurred while checking enrollment" });
        }
    }
}