using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for generating a QR code for an active session.
/// </summary>
public class QrCodeRequest
{
    /// <summary>
    /// The session ID this QR code belongs to.
    /// Session must be in 'active' status.
    /// </summary>
    [Required]
    public int SessionId { get; set; }

    /// <summary>
    /// How many minutes until the QR code expires (1-1440 minutes / 24 hours)
    /// </summary>
    [Required]
    [Range(1, 1440, ErrorMessage = "Expiration minutes must be between 1 and 1440 (24 hours)")]
    public int ExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of times this QR code can be scanned (null = unlimited)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Max usage must be greater than 0")]
    public int? MaxUsage { get; set; }

    /// <summary>
    /// Client-provided unique hash component for additional entropy in QR code generation.
    /// This value is combined with server-generated randomness to create the final QR hash,
    /// ensuring uniqueness and preventing collisions.
    ///
    /// Recommended formats:
    /// - UUID-based: "uuid-{guid}" (e.g., "uuid-123e4567-e89b-12d3-a456-426614174000")
    /// - Timestamp-based: "ts-{timestamp}" (e.g., "ts-1635789012345")
    /// - Custom: Any unique string up to 255 characters
    ///
    /// Purpose: Provides client-side control over QR code uniqueness and enables
    /// different QR codes for the same session when needed.
    /// </summary>
    /// <example>uuid-123e4567-e89b-12d3-a456-426614174000</example>
    /// <example>ts-1635789012345-instructor-001</example>
    [Required]
    [StringLength(255)]
    public string UniqueHash { get; set; } = string.Empty;
}
