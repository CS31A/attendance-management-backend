using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for creating a QR code with explicit parameters.
/// </summary>
public class CreateQrCode
{
    /// <summary>
    /// The session ID this QR code belongs to
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// The session UUID this QR code belongs to (alternative to SessionId)
    /// </summary>
    public Guid? SessionUuid { get; set; }

    /// <summary>
    /// The QR code hash value
    /// </summary>
    [Required]
    [StringLength(255)]
    public string QrHash { get; set; } = string.Empty;

    /// <summary>
    /// When this QR code expires
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of scans allowed (null = unlimited)
    /// </summary>
    public int? MaxUsage { get; set; }
}