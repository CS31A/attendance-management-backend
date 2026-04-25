using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using attendance_monitoring.Constants;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

/// <summary>
/// Represents an actual class session occurrence (instance of a recurring schedule).
/// Sessions track when a class actually happens, including start/end times and room changes.
/// </summary>
[Index(nameof(ScheduleId))]
[Index(nameof(SessionDate))]
[Index(nameof(Status))]
[Index(nameof(ScheduleId), nameof(SessionDate))]
public class Session
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the recurring schedule this session belongs to
    /// </summary>
    [Required]
    public Guid ScheduleId { get; set; }

    /// <summary>
    /// Current status of the session
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = SessionStatusConstants.NotStarted;

    /// <summary>
    /// The date this session occurred (without time component)
    /// </summary>
    [Required]
    [Column(TypeName = "date")]
    public DateTime SessionDate { get; set; }

    /// <summary>
    /// When the instructor actually started the session (null if not started)
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// When the instructor actually ended the session (null if not ended)
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// Cutoff time for marking attendance (typically 15 minutes after start)
    /// Students checking in after this are marked as "Late"
    /// </summary>
    public DateTime? AttendanceCutOff { get; set; }

    /// <summary>
    /// Optional description or notes about this session
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The actual room where the session took place (may differ from scheduled room)
    /// Null if session hasn't started yet
    /// </summary>
    public Guid? ActualRoomId { get; set; }

    /// <summary>
    /// Foreign key to the instructor who started the session
    /// </summary>
    public Guid? StartedBy { get; set; }

    /// <summary>
    /// Foreign key to the instructor who ended the session
    /// </summary>
    public Guid? EndedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Row version token used for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    [ForeignKey("ScheduleId")]
    [JsonIgnore]
    public Schedules Schedule { get; set; } = null!;

    [ForeignKey("ActualRoomId")]
    [JsonIgnore]
    public Classroom? ActualRoom { get; set; }

    [ForeignKey("StartedBy")]
    [JsonIgnore]
    public Instructor? InstructorWhoStarted { get; set; }

    [ForeignKey("EndedBy")]
    [JsonIgnore]
    public Instructor? InstructorWhoEnded { get; set; }

    /// <summary>
    /// QR codes generated for this session
    /// </summary>
    [JsonIgnore]
    public ICollection<QrCode> QrCodes { get; set; } = new List<QrCode>();

    /// <summary>
    /// Attendance records for this session
    /// </summary>
    [JsonIgnore]
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
