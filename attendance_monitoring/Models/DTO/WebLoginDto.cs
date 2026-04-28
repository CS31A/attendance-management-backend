using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO;

public class WebLoginDto
{
    /// <summary>
    /// Email or username for login
    /// </summary>
    [Required(ErrorMessage = "Email or username is required")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Password for login
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}