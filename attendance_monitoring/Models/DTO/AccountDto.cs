using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO;

public class RegisterDto
{
    /// <summary>
    /// Username for the new account
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, ErrorMessage = "Username must be between 3 and 50 characters", MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// First name of the user (optional)
    /// </summary>
    public string? Firstname { get; set; }

    /// <summary>
    /// Last name of the user (optional)
    /// </summary>
    public string? Lastname { get; set; }

    /// <summary>
    /// Email address for the new account
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [AllowedEmailDomains(ErrorMessage = "Email domain is not allowed. Please use an email address from an allowed domain.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password for the new account
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the password
    /// </summary>
    [Required(ErrorMessage = "Repeated password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string RepeatedPassword { get; set; } = string.Empty;

    /// <summary>
    /// User role - valid values are: "Student", "Teacher", "Admin"
    /// </summary>
    [RegularExpression("^(Student|Teacher|Instructor|Admin)$", ErrorMessage = "Invalid role specified. Valid roles are: Student, Teacher, Instructor, Admin")]
    public string? Role { get; set; }

    /// <summary>
    /// Section ID for student registration (required only for students)
    /// </summary>
    public int? SectionId { get; set; }
}

public class LoginDto
{
    /// <summary>
    /// Email or username for login
    /// </summary>
    [Required(ErrorMessage = "Email or username is required")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for login
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
