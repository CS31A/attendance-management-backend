using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for creating a new attendance record manually.
/// </summary>
public class CreateAttendanceRequest
{
    /// <summary>
    /// The ID of the student for this attendance record.
    /// </summary>
    [Required(ErrorMessage = "Student ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Student ID must be a positive integer")]
    public int StudentId { get; set; }

    /// <summary>
    /// The ID of the session for this attendance record.
    /// </summary>
    [Required(ErrorMessage = "Session ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Session ID must be a positive integer")]
    public int SessionId { get; set; }

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
    /// Optional check-in time. Defaults to current UTC time if not provided.
    /// </summary>
    public DateTime? CheckInTime { get; set; }
}
