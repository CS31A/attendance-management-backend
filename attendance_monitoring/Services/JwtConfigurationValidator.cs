using System.Text;
using attendance_monitoring.Constants;

namespace attendance_monitoring.Services;

/// <summary>
/// Service for validating JWT configuration at application startup
/// </summary>
public static class JwtConfigurationValidator
{
    /// <summary>
    /// Validates JWT configuration settings and throws detailed exceptions if invalid
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is missing or invalid</exception>
    public static void ValidateJwtConfiguration(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate Token
        var token = configuration[TokenConstants.JwtTokenConfigKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            errors.Add($"JWT Token is missing or empty. Please set '{TokenConstants.JwtTokenConfigKey}' in appsettings.json or environment variables.");
        }
        else
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            if (tokenBytes.Length < TokenConstants.MinimumJwtKeyLengthBytes)
            {
                errors.Add($"JWT Token is too short. Minimum length is {TokenConstants.MinimumJwtKeyLengthBytes} bytes ({TokenConstants.MinimumJwtKeyLengthBytes * 8} bits) for HMAC-SHA256 security. Current length: {tokenBytes.Length} bytes ({tokenBytes.Length * 8} bits).");
            }
        }

        // Validate Issuer
        var issuer = configuration[TokenConstants.JwtIssuerConfigKey];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            errors.Add($"JWT Issuer is missing or empty. Please set '{TokenConstants.JwtIssuerConfigKey}' in appsettings.json or environment variables.");
        }

        // Validate Audience
        var audience = configuration[TokenConstants.JwtAudienceConfigKey];
        if (string.IsNullOrWhiteSpace(audience))
        {
            errors.Add($"JWT Audience is missing or empty. Please set '{TokenConstants.JwtAudienceConfigKey}' in appsettings.json or environment variables.");
        }

        // If any errors, throw exception with all error messages
        if (errors.Count > 0)
        {
            var errorMessage = "JWT Configuration Validation Failed:\n" + string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Gets the validated JWT token from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>JWT token string</returns>
    /// <exception cref="InvalidOperationException">Thrown when token is missing or invalid</exception>
    public static string GetValidatedToken(IConfiguration configuration)
    {
        var token = configuration[TokenConstants.JwtTokenConfigKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException($"JWT Token is missing. Please set '{TokenConstants.JwtTokenConfigKey}' in configuration.");
        }

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        if (tokenBytes.Length < TokenConstants.MinimumJwtKeyLengthBytes)
        {
            throw new InvalidOperationException($"JWT Token is too short. Minimum length is {TokenConstants.MinimumJwtKeyLengthBytes} bytes for security. Current: {tokenBytes.Length} bytes.");
        }

        return token;
    }

    /// <summary>
    /// Gets the validated JWT issuer from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>JWT issuer string</returns>
    /// <exception cref="InvalidOperationException">Thrown when issuer is missing</exception>
    public static string GetValidatedIssuer(IConfiguration configuration)
    {
        var issuer = configuration[TokenConstants.JwtIssuerConfigKey];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException($"JWT Issuer is missing. Please set '{TokenConstants.JwtIssuerConfigKey}' in configuration.");
        }
        return issuer;
    }

    /// <summary>
    /// Gets the validated JWT audience from configuration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>JWT audience string</returns>
    /// <exception cref="InvalidOperationException">Thrown when audience is missing</exception>
    public static string GetValidatedAudience(IConfiguration configuration)
    {
        var audience = configuration[TokenConstants.JwtAudienceConfigKey];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException($"JWT Audience is missing. Please set '{TokenConstants.JwtAudienceConfigKey}' in configuration.");
        }
        return audience;
    }
}
