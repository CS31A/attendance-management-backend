using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for creating a new session.
/// </summary>
public class CreateSession
{
    /// <summary>
    /// The ID of the schedule for which this session is being created.
    /// </summary>
    [Required(ErrorMessage = "Schedule ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Schedule ID must be a positive integer")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// The date when the session will take place.
    /// If not provided, defaults to the current date.
    /// </summary>
    public DateTime? SessionDate { get; set; }

    /// <summary>
    /// Optional description or notes for the session.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Allows creating a session even when the date does not match the schedule day of week.
    /// Must be paired with <see cref="OffScheduleReason"/> when used.
    /// </summary>
    public bool AllowOffScheduleDate { get; set; }

    /// <summary>
    /// Reason for allowing an off-schedule session date.
    /// Required when <see cref="AllowOffScheduleDate"/> is true and date mismatches schedule day.
    /// </summary>
    [MinLength(5, ErrorMessage = "Off-schedule reason must be at least 5 characters")]
    [MaxLength(500, ErrorMessage = "Off-schedule reason cannot exceed 500 characters")]
    public string? OffScheduleReason { get; set; }
}
