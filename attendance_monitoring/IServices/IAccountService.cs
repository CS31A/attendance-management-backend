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
        Task<(TokenResponseDto?, string?)> LoginAsync(LoginDto loginDto);
        Task<(TokenResponseDto?, string?)> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);
        Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);
    }
}
