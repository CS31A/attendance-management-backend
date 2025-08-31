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
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task<RefreshToken?> GetByReplacedTokenHashAsync(string replacedTokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.ReplacedByTokenHash == replacedTokenHash);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        var entry = await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .AnyAsync(rt => rt.TokenHash == tokenHash);
    }
}