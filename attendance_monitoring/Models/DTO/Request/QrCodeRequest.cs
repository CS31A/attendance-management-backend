using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class QrCodeRequest
{
    [Required]
    public int ScheduleId { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public int ActualRoomId { get; set; }

    [Required]
    [Range(1, 1440, ErrorMessage = "Expiration minutes must be between 1 and 1440 (24 hours)")]
    public int ExpirationMinutes { get; set; } = 30;

    [Range(1, int.MaxValue, ErrorMessage = "Max usage must be greater than 0")]
    public int? MaxUsage { get; set; }

    [Required]
    [StringLength(255)]
    public string UniqueHash { get; set; } = string.Empty;
}
