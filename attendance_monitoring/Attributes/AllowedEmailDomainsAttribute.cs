using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Attributes;

/// <summary>
/// Validation attribute to check if an email address uses an allowed domain
/// </summary>
public class AllowedEmailDomainsAttribute : ValidationAttribute
{
    private readonly string _allowedDomainsKey = "AllowedEmailDomains";

    /// <summary>
    /// Validates that the email address uses an allowed domain
    /// </summary>
    /// <param name="value">The email address to validate</param>
    /// <param name="validationContext">The validation context</param>
    /// <returns>Validation result</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var email = value as string;

        // If the email is null or empty, the validation passes because this is an optional field.
        // Use [Required] for mandatory fields.
        if (string.IsNullOrEmpty(email))
        {
            return ValidationResult.Success;
        }

        // Get allowed domains from configuration
        var configuration = (IConfiguration?)validationContext.GetService(typeof(IConfiguration));
        var allowedDomains = configuration?.GetSection(_allowedDomainsKey).Get<string[]>() ?? Array.Empty<string>();

        // If no domains are configured, allow all (fallback behavior)
        if (allowedDomains.Length == 0)
        {
            return ValidationResult.Success;
        }

        // Extract domain from email
        var emailParts = email.Split('@');
        if (emailParts.Length != 2)
        {
            return new ValidationResult(ErrorMessage ?? "Invalid email format");
        }

        var domain = emailParts[1].ToLowerInvariant();

        // Check if domain is in allowed list
        if (allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? $"Email domain '{domain}' is not allowed. Please use an email address from an allowed domain.");
    }
}
