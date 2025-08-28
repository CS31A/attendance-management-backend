using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO;

public class RevokeTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}