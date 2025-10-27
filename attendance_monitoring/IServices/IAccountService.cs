using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.IServices
{
    public interface IAccountService
    {
        Task<(IdentityResult, RegisterResponseDto?)> RegisterAsync(RegisterDto registerDto);
        Task<(TokenResponseDto?, string?, string?)> LoginAsync(LoginDto loginDto);
        Task<(TokenResponseDto?, string?)> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);
        Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);
        Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken);
        Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken);
        Task BlacklistTokenAsync(string jti, DateTime expiresAt);
        Task<(UserProfileResponseDto?, string?)> GetUserProfileAsync(string userId);
    }
}
