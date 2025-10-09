using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    #region Read Operations

    #region GetByTokenHashAsync
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }
    #endregion

    #region GetByReplacedTokenHashAsync
    public async Task<RefreshToken?> GetByReplacedTokenHashAsync(string replacedTokenHash)
    {
        return await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.ReplacedByTokenHash == replacedTokenHash).ConfigureAwait(false);
    }
    #endregion

    #region ExistsAsync
    public async Task<bool> ExistsAsync(string tokenHash)
    {
        return await context.RefreshTokens
            .AnyAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateAsync
    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        var entry = await context.RefreshTokens.AddAsync(refreshToken).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateAsync
    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        context.RefreshTokens.Update(refreshToken);
    }
    #endregion

    #endregion

    #region Utility Operations

    #region SaveChangesAsync
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
    #endregion

    #endregion
}
