using attendance_monitoring.Classes;
using System;
using System.Threading.Tasks;

namespace attendance_monitoring.IRepository;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task<bool> ExistsAsync(string tokenHash);
}