using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for updating the actual room of an active session.
/// </summary>
public class UpdateSessionRoom : IValidatableObject
{
    /// <summary>
    /// The UUID of the new classroom for the session.
    /// </summary>
    public Guid? ActualRoomId { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the current session row.
    /// Serialized as base64 over JSON.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ActualRoomId.HasValue)
        {
            yield return new ValidationResult(
                "ActualRoomId is required.",
                [nameof(ActualRoomId)]);
        }
    }
}
