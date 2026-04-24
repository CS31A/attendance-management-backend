using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for updating the actual room of an active session.
/// </summary>
public class UpdateSessionRoom : IValidatableObject
{
    /// <summary>
    /// The new classroom ID for the session.
    /// The classroom must exist and be available.
    /// </summary>
    public int? ActualRoomId { get; set; }

    /// <summary>
    /// The UUID of the new classroom for the session.
    /// </summary>
    public Guid? ActualRoomUuid { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the current session row.
    /// Serialized as base64 over JSON.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ActualRoomId.HasValue && !ActualRoomUuid.HasValue)
        {
            yield return new ValidationResult(
                "Either ActualRoomId or ActualRoomUuid is required.",
                [nameof(ActualRoomId), nameof(ActualRoomUuid)]);
        }

        if (ActualRoomId.HasValue && ActualRoomId.Value <= 0)
        {
            yield return new ValidationResult(
                "ActualRoomId must be a valid positive integer.",
                [nameof(ActualRoomId)]);
        }
    }
}
