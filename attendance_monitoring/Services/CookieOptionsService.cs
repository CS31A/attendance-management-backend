using Microsoft.AspNetCore.Http;
using attendance_monitoring.IServices;

namespace attendance_monitoring.Services;

/// <summary>
/// Service for creating standardized cookie options to eliminate code duplication
/// </summary>
public class CookieOptionsService(IConfiguration configuration) : ICookieOptionsService
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>
    /// Creates cookie options for access tokens
    /// </summary>
    /// <returns>Configured CookieOptions for access tokens</returns>
    public CookieOptions CreateAccessTokenCookieOptions()
    {
        var accessTokenExpirationMinutes = _configuration.GetValue("CookieSettings:AccessTokenExpirationMinutes", 15);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production with HTTPS
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes)
        };
    }

    /// <summary>
    /// Creates cookie options for refresh tokens
    /// </summary>
    /// <returns>Configured CookieOptions for refresh tokens</returns>
    public CookieOptions CreateRefreshTokenCookieOptions()
    {
        var refreshTokenExpirationDays = _configuration.GetValue("CookieSettings:RefreshTokenExpirationDays", 7);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production with HTTPS
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
        };
    }

    /// <summary>
    /// Creates cookie options for deleting/clearing cookies
    /// </summary>
    /// <returns>Configured CookieOptions for cookie deletion</returns>
    public CookieOptions CreateDeleteCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1) // Set to past date to delete
        };
    }

    /// <summary>
    /// Sets access token cookie with standardized options
    /// </summary>
    /// <param name="response">HTTP response to append cookie to</param>
    /// <param name="accessToken">Access token value</param>
    public void SetAccessTokenCookie(HttpResponse response, string accessToken)
    {
        var cookieOptions = CreateAccessTokenCookieOptions();
        response.Cookies.Append("accessToken", accessToken, cookieOptions);
    }

    /// <summary>
    /// Sets refresh token cookie with standardized options
    /// </summary>
    /// <param name="response">HTTP response to append cookie to</param>
    /// <param name="refreshToken">Refresh token value</param>
    public void SetRefreshTokenCookie(HttpResponse response, string refreshToken)
    {
        var cookieOptions = CreateRefreshTokenCookieOptions();
        response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Sets both access and refresh token cookies
    /// </summary>
    /// <param name="response">HTTP response to append cookies to</param>
    /// <param name="accessToken">Access token value</param>
    /// <param name="refreshToken">Refresh token value</param>
    public void SetTokenCookies(HttpResponse response, string accessToken, string refreshToken)
    {
        SetAccessTokenCookie(response, accessToken);
        SetRefreshTokenCookie(response, refreshToken);
    }

    /// <summary>
    /// Clears access token cookie
    /// </summary>
    /// <param name="response">HTTP response to clear cookie from</param>
    public void ClearAccessTokenCookie(HttpResponse response)
    {
        var cookieOptions = CreateDeleteCookieOptions();
        response.Cookies.Append("accessToken", string.Empty, cookieOptions);
    }

    /// <summary>
    /// Clears refresh token cookie
    /// </summary>
    /// <param name="response">HTTP response to clear cookie from</param>
    public void ClearRefreshTokenCookie(HttpResponse response)
    {
        var cookieOptions = CreateDeleteCookieOptions();
        response.Cookies.Append("refreshToken", string.Empty, cookieOptions);
    }

    /// <summary>
    /// Clears both access and refresh token cookies
    /// </summary>
    /// <param name="response">HTTP response to clear cookies from</param>
    public void ClearTokenCookies(HttpResponse response)
    {
        ClearAccessTokenCookie(response);
        ClearRefreshTokenCookie(response);
    }
}