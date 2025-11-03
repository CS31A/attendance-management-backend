namespace attendance_monitoring.Constants;

/// <summary>
/// Constants for token configuration to avoid magic numbers
/// </summary>
public static class TokenConstants
{
    /// <summary>
    /// Access token expiration time in minutes
    /// </summary>
    public const int AccessTokenExpirationMinutes = 15;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public const int RefreshTokenExpirationDays = 7;

    /// <summary>
    /// Refresh token length in bytes (256 bits)
    /// </summary>
    public const int RefreshTokenLength = 32;

    /// <summary>
    /// Minimum JWT signing key length in bytes (256 bits for HMAC-SHA256 security)
    /// </summary>
    public const int MinimumJwtKeyLengthBytes = 32;

    /// <summary>
    /// Configuration key for JWT token
    /// </summary>
    public const string JwtTokenConfigKey = "AppSettings:Token";

    /// <summary>
    /// Configuration key for JWT issuer
    /// </summary>
    public const string JwtIssuerConfigKey = "AppSettings:Issuer";

    /// <summary>
    /// Configuration key for JWT audience
    /// </summary>
    public const string JwtAudienceConfigKey = "AppSettings:Audience";
}
