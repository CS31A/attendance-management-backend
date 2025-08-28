using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace attendance_monitoring.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private const int RefreshTokenLength = 32; // 256 bits

    public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, UserManager<IdentityUser> userManager)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[RefreshTokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashedBytes);
    }

    public async Task<(RefreshToken, string)> CreateRefreshTokenAsync(string userId)
    {
        var refreshToken = await GenerateRefreshTokenAsync();
        var tokenHash = HashRefreshToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days from now
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return (refreshTokenEntity, refreshToken);
    }

    public async Task<(RefreshToken?, string?)> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null)
        {
            return (null, "Invalid refresh token");
        }

        if (storedToken.IsRevoked)
        {
            // TODO: 
            // This is a security event - token reuse detected
            // In a full implementation, we would revoke the entire token family
            return (null, "Refresh token has been revoked");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return (null, "Refresh token has expired");
        }

        return (storedToken, null);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string userId)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken != null && storedToken.UserId == userId)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(storedToken);
            return true;
        }

        return false;
    }

    public async Task<(RefreshToken?, string?)> RotateRefreshTokenAsync(string oldRefreshToken, string userId)
    {
        // Validate the old token first
        var (storedToken, validationError) = await ValidateRefreshTokenAsync(oldRefreshToken);
        
        if (storedToken == null)
        {
            return (null, validationError);
        }

        // Mark the old token as revoked
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        
        // Create a new refresh token
        var (newRefreshTokenEntity, newRefreshToken) = await CreateRefreshTokenAsync(userId);
        
        // Link the old token to the new one
        storedToken.ReplacedByTokenHash = newRefreshTokenEntity.TokenHash;
        
        // Update the old token
        await _refreshTokenRepository.UpdateAsync(storedToken);
        
        return (newRefreshTokenEntity, newRefreshToken);
    }
}