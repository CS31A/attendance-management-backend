using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(EventId), IsUnique = true)]
[Index(nameof(DeviceId), nameof(CapturedAt))]
[Index(nameof(MatchedStudentId), nameof(CapturedAt))]
[Index(nameof(Status))]
public class FingerprintScanEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid EventId { get; set; } = Guid.NewGuid();

    [Required]
    public int DeviceId { get; set; }

    public int? MatchedStudentId { get; set; }

    public int? SessionId { get; set; }

    public int? AttendanceRecordId { get; set; }

    [Precision(5, 4)]
    public decimal? MatchScore { get; set; }

    [Precision(5, 4)]
    public decimal? ThresholdUsed { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? FailureReason { get; set; }

    [StringLength(128)]
    public string? PayloadHash { get; set; }

    public DateTime CapturedAt { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey(nameof(DeviceId))]
    [JsonIgnore]
    public FingerprintDevice Device { get; set; } = null!;

    [ForeignKey(nameof(MatchedStudentId))]
    [JsonIgnore]
    public Student? MatchedStudent { get; set; }

    [ForeignKey(nameof(SessionId))]
    [JsonIgnore]
    public Session? Session { get; set; }

    [ForeignKey(nameof(AttendanceRecordId))]
    [JsonIgnore]
    public AttendanceRecord? AttendanceRecord { get; set; }
}
