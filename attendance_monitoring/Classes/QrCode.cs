using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

/// <summary>
/// Represents a QR code generated for a specific session.
/// QR codes are ephemeral and tied to a session instance.
/// Session contains the contextual data (schedule, room, section).
/// </summary>
[Index(nameof(SessionId))]
[Index(nameof(IsActive))]
[Index(nameof(ExpiresAt))]
public class QrCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the session this QR code belongs to.
    /// Session contains: ScheduleId, ActualRoomId, SectionId (via Schedule)
    /// </summary>
    [Required]
    public int SessionId { get; set; }

    /// <summary>
    /// Unique hash for QR code validation
    /// </summary>
    [Required]
    [StringLength(255)]
    public string QrHash { get; set; } = string.Empty;

    /// <summary>
    /// When this QR code was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When this QR code expires (typically 15-30 minutes after generation)
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of times this QR can be scanned (null = unlimited)
    /// </summary>
    public int? MaxUsage { get; set; }

    /// <summary>
    /// Whether this QR code is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of times this QR code has been scanned
    /// </summary>
    public int UsageCount { get; set; } = 0;

    // Revocation audit trail
    /// <summary>
    /// When this QR code was revoked (null if not revoked)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// User ID of who revoked this QR code
    /// </summary>
    [StringLength(256)]
    public string? RevokedBy { get; set; }

    /// <summary>
    /// Reason for revoking this QR code
    /// </summary>
    [StringLength(500)]
    public string? RevocationReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token for detecting conflicting updates.
    /// SQL Server will manage this automatically as a rowversion.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    [ForeignKey("SessionId")]
    [JsonIgnore]
    public Session Session { get; set; } = null!;

    /// <summary>
    /// Attendance records created by scanning this QR code
    /// </summary>
    [JsonIgnore]
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
