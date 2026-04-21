using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

internal interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginDto loginDto);
    Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);
    Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);
    Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken);
    Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken);
    Task BlacklistTokenAsync(string jti, DateTime expiresAt);
}
