using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetByReplacedTokenHashAsync(string replacedTokenHash)
    {
        return await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.ReplacedByTokenHash == replacedTokenHash).ConfigureAwait(false);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        var entry = await context.RefreshTokens.AddAsync(refreshToken).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        context.RefreshTokens.Update(refreshToken);
    }

    public async Task<bool> ExistsAsync(string tokenHash)
    {
        return await context.RefreshTokens
            .AnyAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
