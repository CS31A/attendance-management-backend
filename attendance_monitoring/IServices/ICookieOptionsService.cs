using Microsoft.AspNetCore.Http;

namespace attendance_monitoring.IServices;

/// <summary>
/// Interface for cookie options service that provides standardized cookie configuration
/// to eliminate code duplication across the application
/// </summary>
public interface ICookieOptionsService
{
    /// <summary>
    /// Creates cookie options for access tokens
    /// </summary>
    /// <returns>Configured CookieOptions for access tokens</returns>
    CookieOptions CreateAccessTokenCookieOptions();

    /// <summary>
    /// Creates cookie options for refresh tokens
    /// </summary>
    /// <returns>Configured CookieOptions for refresh tokens</returns>
    CookieOptions CreateRefreshTokenCookieOptions();

    /// <summary>
    /// Creates cookie options for deleting/clearing cookies
    /// </summary>
    /// <returns>Configured CookieOptions for cookie deletion</returns>
    CookieOptions CreateDeleteCookieOptions();

    /// <summary>
    /// Sets access token cookie with standardized options
    /// </summary>
    /// <param name="response">HTTP response to append cookie to</param>
    /// <param name="accessToken">Access token value</param>
    void SetAccessTokenCookie(HttpResponse response, string accessToken);

    /// <summary>
    /// Sets refresh token cookie with standardized options
    /// </summary>
    /// <param name="response">HTTP response to append cookie to</param>
    /// <param name="refreshToken">Refresh token value</param>
    void SetRefreshTokenCookie(HttpResponse response, string refreshToken);

    /// <summary>
    /// Sets both access and refresh token cookies
    /// </summary>
    /// <param name="response">HTTP response to append cookies to</param>
    /// <param name="accessToken">Access token value</param>
    /// <param name="refreshToken">Refresh token value</param>
    void SetTokenCookies(HttpResponse response, string accessToken, string refreshToken);

    /// <summary>
    /// Clears access token cookie
    /// </summary>
    /// <param name="response">HTTP response to clear cookie from</param>
    void ClearAccessTokenCookie(HttpResponse response);

    /// <summary>
    /// Clears refresh token cookie
    /// </summary>
    /// <param name="response">HTTP response to clear cookie from</param>
    void ClearRefreshTokenCookie(HttpResponse response);

    /// <summary>
    /// Clears both access and refresh token cookies
    /// </summary>
    /// <param name="response">HTTP response to clear cookies from</param>
    void ClearTokenCookies(HttpResponse response);
}