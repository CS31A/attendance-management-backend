using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for users updating their own profile information
/// </summary>
public class UpdateProfile
{
    /// <summary>
    /// User's first name (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string? Firstname { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string? Lastname { get; set; }

    /// <summary>
    /// User's email address (optional)
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [AllowedEmailDomains(ErrorMessage = "Email domain is not allowed")]
    public string? Email { get; set; }

    /// <summary>
    /// Current password (required if changing password)
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// New password (required if changing password)
    /// </summary>
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    public string? NewPassword { get; set; }

    /// <summary>
    /// Confirmation of new password (must match NewPassword)
    /// </summary>
    [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match")]
    public string? ConfirmNewPassword { get; set; }

    /// <summary>
    /// For students: Section ID (UUID value serialized as a string in the public API)
    /// </summary>
    public Guid? SectionId { get; set; }

    /// <summary>
    /// For students: Regular/Irregular status (optional)
    /// </summary>
    public bool? IsRegular { get; set; }
}
