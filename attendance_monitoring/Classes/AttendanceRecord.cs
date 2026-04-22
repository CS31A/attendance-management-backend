using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

/// <summary>
/// Represents a student's attendance record for a specific session.
/// Tracks when a student checked in and their attendance status.
/// </summary>
[Index(nameof(StudentId))]
[Index(nameof(SessionId))]
[Index(nameof(CheckInTime))]
[Index(nameof(Status))]
public class AttendanceRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid Uuid { get; set; }

    /// <summary>
    /// Foreign key to the student who attended
    /// </summary>
    [Required]
    public int StudentId { get; set; }

    /// <summary>
    /// Foreign key to the session this attendance is for
    /// </summary>
    [Required]
    public int SessionId { get; set; }

    /// <summary>
    /// Foreign key to the QR code that was scanned (null if manual entry)
    /// </summary>
    public int? QrCodeId { get; set; }

    /// <summary>
    /// When the student checked in (scanned QR or manually marked)
    /// </summary>
    [Required]
    public DateTime CheckInTime { get; set; }

    /// <summary>
    /// Attendance status
    /// Values: "Present", "Late", "Excused", "Absent"
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Present";

    /// <summary>
    /// Optional notes about this attendance record
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this was manually entered by an instructor (true) or scanned via QR (false)
    /// </summary>
    public bool IsManualEntry { get; set; } = false;

    /// <summary>
    /// User ID of the person who entered this record (if manual entry)
    /// </summary>
    [StringLength(256)]
    public string? EnteredBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    [JsonIgnore]
    public Student Student { get; set; } = null!;

    [ForeignKey("SessionId")]
    [JsonIgnore]
    public Session Session { get; set; } = null!;

    [ForeignKey("QrCodeId")]
    [JsonIgnore]
    public QrCode? QrCode { get; set; }
}
