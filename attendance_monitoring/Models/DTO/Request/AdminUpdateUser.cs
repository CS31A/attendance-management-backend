using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for admins updating other users' profiles
/// </summary>
public class AdminUpdateUser
{
    /// <summary>
    /// Target user's ID (required to identify user)
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public string UserId { get; set; } = string.Empty;

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
    /// New password for password reset (optional, admin doesn't need current password)
    /// </summary>
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string? NewPassword { get; set; }

    /// <summary>
    /// For students: Section ID (optional)
    /// </summary>
    public int? SectionId { get; set; }

    /// <summary>
    /// For students: Regular/Irregular status (optional)
    /// </summary>
    public bool? IsRegular { get; set; }

    /// <summary>
    /// Account active status (optional - for soft delete management)
    /// </summary>
    public bool? IsDeleted { get; set; }
}
