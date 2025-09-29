using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace attendance_monitoring.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetByReplacedTokenHashAsync(string replacedTokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.ReplacedByTokenHash == replacedTokenHash).ConfigureAwait(false);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        var entry = await _context.RefreshTokens.AddAsync(refreshToken).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .AnyAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
    }

    public async Task<List<RefreshToken>> GetExpiredTokensAsync(DateTime currentDateTime, int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < currentDateTime || 
                       (rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < currentDateTime.AddDays(-30)))
            .OrderBy(rt => rt.ExpiresAt) // Ensure predictable ordering
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> RemoveTokensAsync(IEnumerable<RefreshToken> tokens, CancellationToken cancellationToken = default)
    {
        var tokenList = tokens.ToList();
        if (!tokenList.Any())
        {
            return 0;
        }

        _context.RefreshTokens.RemoveRange(tokenList);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tokenList.Count;
    }
}
