using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class ScanFingerprintBySensorRequest
{
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public int SensorFingerprintId { get; set; }

    [Range(0, 255)]
    public int Confidence { get; set; }

    public int? SessionId { get; set; }

    public DateTime? CapturedAt { get; set; }
}
