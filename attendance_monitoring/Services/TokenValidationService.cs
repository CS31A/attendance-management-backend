using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly ApplicationDbContext _context;

    public TokenValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        return await _context.BlacklistedTokens.AnyAsync(bt => bt.Jti == jti);
    }
}