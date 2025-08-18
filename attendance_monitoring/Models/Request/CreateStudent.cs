using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request;

public class CreateStudent
{
    [Required]
    [StringLength(100)]
    public string Firstname { get; set; }

    [Required]
    [StringLength(100)]
    public string Lastname { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}