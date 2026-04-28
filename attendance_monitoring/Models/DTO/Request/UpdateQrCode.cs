using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for updating a QR code.
/// Note: ActualRoomId is managed at the Session level, not QR code level.
/// </summary>
public class UpdateQrCode
{
    /// <summary>
    /// Update the expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Activate or deactivate the QR code
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Update the maximum usage limit
    /// </summary>
    public int? MaxUsage { get; set; }
}