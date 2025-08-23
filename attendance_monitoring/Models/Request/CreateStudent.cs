using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

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
    [AllowedEmailDomains(ErrorMessage = "Email domain is not allowed. Please use an email address from an allowed domain.")]
    public string Email { get; set; }
}