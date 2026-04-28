using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The current access token to be revoked.
    /// Including this value will blacklist the token when issuing a new one.
    /// </summary>
    public string? OldAccessToken { get; set; }
}