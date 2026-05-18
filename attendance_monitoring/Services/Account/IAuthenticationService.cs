using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginDto loginDto);
    Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);
    Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);
    Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken);
    Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken);
    Task BlacklistTokenAsync(string jti, DateTime expiresAt);
}

/// <summary>
/// Result class for login operations.
/// </summary>
public class LoginResult
{
    /// <summary>
    /// The token response containing access and refresh tokens.
    /// </summary>
    public required TokenResponseDto TokenResponse { get; set; }

    /// <summary>
    /// The authenticated user's username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The authenticated user's role.
    /// </summary>
    public required string Role { get; set; }
}
