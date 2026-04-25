using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for creating a new attendance record manually.
/// </summary>
public class CreateAttendanceRequest : IValidatableObject
{
    /// <summary>
    /// The UUID of the student for this attendance record.
    /// </summary>
    public Guid? StudentId { get; set; }

    /// <summary>
    /// The UUID of the session for this attendance record.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Attendance status: Present, Late, Excused, or Absent.
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    [RegularExpression("^(Present|Late|Excused|Absent)$", ErrorMessage = "Status must be Present, Late, Excused, or Absent")]
    public string Status { get; set; } = "Present";

    /// <summary>
    /// Optional notes about this attendance record.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Optional check-in time. Defaults to current local time if not provided.
    /// </summary>
    public DateTime? CheckInTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!StudentId.HasValue)
        {
            yield return new ValidationResult(
                "StudentId is required.",
                [nameof(StudentId)]);
        }

        if (!SessionId.HasValue)
        {
            yield return new ValidationResult(
                "SessionId is required.",
                [nameof(SessionId)]);
        }
    }
}
