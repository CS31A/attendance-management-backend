using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for ending a session.
/// </summary>
public class EndSession
{
    /// <summary>
    /// Optional description or notes about the session upon completion.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the current session row.
    /// Serialized as base64 over JSON.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
