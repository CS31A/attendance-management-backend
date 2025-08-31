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
}
