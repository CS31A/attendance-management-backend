using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

/// <summary>
/// Represents additional subject enrollments for irregular students beyond their primary section.
/// Regular students primarily use their Section relationship, while irregular students use this
/// for cross-section enrollments, retakes, and additional subjects.
/// </summary>
[Index(nameof(StudentId), nameof(SectionId), nameof(SubjectId), IsUnique = true)]
[Index(nameof(StudentId))]
[Index(nameof(SectionId))]
[Index(nameof(SubjectId))]
[Index(nameof(IsActive))]
public class StudentEnrollment
{
    [Key]
    public Guid Id { get; set; }

    // Foreign key to Student
    [Required]
    public Guid StudentId { get; set; }

    // Foreign key to Section  
    [Required]
    public Guid SectionId { get; set; }

    // Foreign key to Subject (for granular control)
    [Required]
    public Guid SubjectId { get; set; }

    // Enrollment status
    public bool IsActive { get; set; } = true;

    // Enrollment type (Regular, Irregular, Retake, etc.)
    [StringLength(20)]
    public string EnrollmentType { get; set; } = "Regular";

    // Academic year and semester for this enrollment
    [StringLength(10)]
    public string? AcademicYear { get; set; }

    [StringLength(20)]
    public string? Semester { get; set; }

    // Enrollment date
    public DateTime EnrolledAt { get; set; }

    // Drop date (if student drops the subject)
    public DateTime? DroppedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    [JsonIgnore]
    public Student Student { get; set; } = null!;

    [ForeignKey("SectionId")]
    [JsonIgnore]
    public Section Section { get; set; } = null!;

    [ForeignKey("SubjectId")]
    [JsonIgnore]
    public Subject Subject { get; set; } = null!;
}
