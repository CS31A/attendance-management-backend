using attendance_monitoring.Classes;
using System.Security.Claims;
using System.Threading.Tasks;

namespace attendance_monitoring.IServices;

public interface IRefreshTokenService
{
    Task<string> GenerateRefreshTokenAsync();
    string HashRefreshToken(string refreshToken);
        Task<(RefreshToken, string)> CreateRefreshTokenAsync(string userId);
    Task<(RefreshToken?, string?)> ValidateRefreshTokenAsync(string refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string userId);
    Task<(RefreshToken?, string?)> RotateRefreshTokenAsync(string oldRefreshToken, string userId);
}