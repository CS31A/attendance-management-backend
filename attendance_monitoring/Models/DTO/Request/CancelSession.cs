using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for cancelling a session.
/// </summary>
public class CancelSession
{
    /// <summary>
    /// The reason for cancelling the session.
    /// </summary>
    [Required(ErrorMessage = "Cancellation reason is required")]
    [MinLength(5, ErrorMessage = "Reason must be at least 5 characters")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
}
