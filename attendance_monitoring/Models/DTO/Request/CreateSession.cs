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
    /// </summary>
    [Required(ErrorMessage = "Session date is required")]
    public DateTime SessionDate { get; set; }

    /// <summary>
    /// Optional description or notes for the session.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
