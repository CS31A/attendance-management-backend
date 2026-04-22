using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(EnrollmentSessionId), IsUnique = true)]
[Index(nameof(DeviceId), nameof(Status))]
[Index(nameof(StudentId), nameof(Status))]
public class FingerprintEnrollmentSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid Uuid { get; set; }

    public Guid EnrollmentSessionId { get; set; } = Guid.NewGuid();

    [Required]
    public int DeviceId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required]
    [StringLength(450)]
    public string RequestedByUserId { get; set; } = string.Empty;

    [Required]
    public int AssignedSensorFingerprintId { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime ExpiresAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(DeviceId))]
    [JsonIgnore]
    public FingerprintDevice Device { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
