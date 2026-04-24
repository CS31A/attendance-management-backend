using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for creating a new attendance record manually.
/// </summary>
public class CreateAttendanceRequest : IValidatableObject
{
    /// <summary>
    /// The ID of the student for this attendance record.
    /// </summary>
    public int? StudentId { get; set; }

    /// <summary>
    /// The UUID of the student for this attendance record.
    /// </summary>
    public Guid? StudentUuid { get; set; }

    /// <summary>
    /// The ID of the session for this attendance record.
    /// </summary>
    public int? SessionId { get; set; }

    /// <summary>
    /// The UUID of the session for this attendance record.
    /// </summary>
    public Guid? SessionUuid { get; set; }

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
        if (!StudentId.HasValue && !StudentUuid.HasValue)
        {
            yield return new ValidationResult(
                "Either StudentId or StudentUuid is required.",
                [nameof(StudentId), nameof(StudentUuid)]);
        }

        if (StudentId.HasValue && StudentId.Value <= 0)
        {
            yield return new ValidationResult(
                "Student ID must be a positive integer.",
                [nameof(StudentId)]);
        }

        if (!SessionId.HasValue && !SessionUuid.HasValue)
        {
            yield return new ValidationResult(
                "Either SessionId or SessionUuid is required.",
                [nameof(SessionId), nameof(SessionUuid)]);
        }

        if (SessionId.HasValue && SessionId.Value <= 0)
        {
            yield return new ValidationResult(
                "Session ID must be a positive integer.",
                [nameof(SessionId)]);
        }
    }
}
