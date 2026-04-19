using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class ValidateQrCode
{
    [Required]
    [StringLength(255)]
    public string QrHash { get; set; } = string.Empty;

    public int? StudentId { get; set; }
}