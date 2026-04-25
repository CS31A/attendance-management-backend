using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

public class StartFingerprintEnrollmentSessionRequest
{
    [Required]
    [NotEmptyGuid(ErrorMessage = "StudentId is required")]
    public Guid StudentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;
}
