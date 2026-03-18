using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class CompleteFingerprintEnrollmentRequest
{
    [Required]
    public Guid EnrollmentSessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public int SensorFingerprintId { get; set; }

    [Required]
    public bool Success { get; set; }

    [StringLength(16000)]
    public string? BackupTemplateBase64 { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }
}
