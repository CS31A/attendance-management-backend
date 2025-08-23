using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request;

public class CreateInstructor
{
    [Required]
    [StringLength(100)]
    public string Firstname { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Lastname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}