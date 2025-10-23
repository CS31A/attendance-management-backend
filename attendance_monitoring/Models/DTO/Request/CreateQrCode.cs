using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class CreateQrCode
{
    [Required]
    public int ScheduleId { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public int ActualRoomId { get; set; }

    [Required]
    [StringLength(255)]
    public string QrHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public int? MaxUsage { get; set; }
}