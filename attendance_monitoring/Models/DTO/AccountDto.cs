using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO;

public class RegisterDto : IValidatableObject
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
    [StringLength(100, ErrorMessage = "Password must be between 8 and 100 characters", MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the password
    /// </summary>
    [Required(ErrorMessage = "Repeated password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string RepeatedPassword { get; set; } = string.Empty;

    /// <summary>
    /// User role - valid values are: "Student", "Instructor", "Admin"
    /// </summary>
    [RegularExpression("^(Student|Instructor|Admin|student|instructor|admin)$", ErrorMessage = "Invalid role specified. Valid roles are: Student, Instructor, Admin")]
    public string? Role { get; set; }

    /// <summary>
    /// Section ID for student registration (UUID string in public API; required only for students)
    /// </summary>
    public Guid? SectionId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Determine effective role (defaults to Student if not specified)
        var effectiveRole = string.IsNullOrEmpty(Role) ? "Student" : Role;

        // Validation 1: Students MUST have a valid SectionId
        if (string.Equals(effectiveRole, "Student", StringComparison.OrdinalIgnoreCase) && (!SectionId.HasValue || SectionId == Guid.Empty))
        {
            yield return new ValidationResult(
                "SectionId is required for student registration",
                new[] { nameof(SectionId) }
            );
        }

        // Validation 2: Non-students MUST NOT have a SectionId
        if (!string.Equals(effectiveRole, "Student", StringComparison.OrdinalIgnoreCase) && SectionId.HasValue)
        {
            yield return new ValidationResult(
                $"SectionId should not be provided for {effectiveRole} role",
                new[] { nameof(SectionId) }
            );
        }
    }
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
