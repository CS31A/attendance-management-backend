using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

public class CreateStudentEnrollment
{
    [Required(ErrorMessage = "StudentId is required")]
    [NotEmptyGuid(ErrorMessage = "StudentId is required")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "SectionId is required")]
    [NotEmptyGuid(ErrorMessage = "SectionId is required")]
    public Guid SectionId { get; set; }

    [Required(ErrorMessage = "SubjectId is required")]
    [NotEmptyGuid(ErrorMessage = "SubjectId is required")]
    public Guid SubjectId { get; set; }

    [RegularExpression("(?i)^(Regular|Irregular|Retake)$", ErrorMessage = "Enrollment type must be one of: Regular, Irregular, Retake")]
    [StringLength(20, ErrorMessage = "Enrollment type must not exceed 20 characters")]
    public string EnrollmentType { get; set; } = "Regular";

    [StringLength(10, ErrorMessage = "Academic year must not exceed 10 characters")]
    public string? AcademicYear { get; set; }

    [StringLength(20, ErrorMessage = "Semester must not exceed 20 characters")]
    public string? Semester { get; set; }
}
