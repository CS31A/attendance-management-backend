using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for updating the actual room of an active session.
/// </summary>
public class UpdateSessionRoom
{
    /// <summary>
    /// The new classroom ID for the session.
    /// The classroom must exist and be available.
    /// </summary>
    [Required(ErrorMessage = "ActualRoomId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "ActualRoomId must be a valid positive integer")]
    public int ActualRoomId { get; set; }
}
