using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class StartFingerprintEnrollmentSessionRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;
}
