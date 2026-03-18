namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for fingerprint information.
/// </summary>
public class FingerprintResponseDto
{
    /// <summary>
    /// The fingerprint ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The associated user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The associated student ID.
    /// </summary>
    public int? StudentId { get; set; }

    /// <summary>
    /// The device ID that registered this fingerprint.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The sensor fingerprint ID.
    /// </summary>
    public int SensorFingerprintId { get; set; }

    /// <summary>
    /// When the fingerprint was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether the fingerprint is active (not deleted).
    /// </summary>
    public bool IsActive { get; set; }
}
