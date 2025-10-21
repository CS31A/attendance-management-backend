using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request model for revoking a QR code with optional reason.
/// </summary>
public class RevokeQrCode
{
    /// <summary>
    /// Optional reason for revoking the QR code.
    /// </summary>
    [StringLength(500, ErrorMessage = "Revocation reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}
