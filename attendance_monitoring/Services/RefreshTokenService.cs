using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, UserManager<IdentityUser> userManager, ILogger<RefreshTokenService> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[TokenConstants.RefreshTokenLength];
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
            ExpiresAt = DateTime.UtcNow.AddDays(TokenConstants.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity).ConfigureAwait(false);

        return (refreshTokenEntity, refreshToken);
    }

    public async Task<(RefreshToken?, string?)> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash).ConfigureAwait(false);

        if (storedToken == null)
        {
            return (null, "Invalid refresh token");
        }

        if (storedToken.IsRevoked)
        {
            // Security event - token reuse detected
            // Revoke the entire token family to prevent further abuse
            await RevokeTokenFamilyAsync(storedToken).ConfigureAwait(false);
            return (null, "Refresh token has been revoked - security violation detected");
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
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash).ConfigureAwait(false);

        if (storedToken != null && storedToken.UserId == userId)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(storedToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task<(RefreshToken?, string?)> RotateRefreshTokenAsync(string oldRefreshToken, string userId)
    {
        // Validate the old token first
        var (storedToken, validationError) = await ValidateRefreshTokenAsync(oldRefreshToken).ConfigureAwait(false);

        if (storedToken == null)
        {
            return (null, validationError);
        }

        // Mark the old token as revoked
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Create a new refresh token
        var (newRefreshTokenEntity, newRefreshToken) = await CreateRefreshTokenAsync(userId).ConfigureAwait(false);

        // Link the old token to the new one
        storedToken.ReplacedByTokenHash = newRefreshTokenEntity.TokenHash;

        // Update the old token
        await _refreshTokenRepository.UpdateAsync(storedToken).ConfigureAwait(false);

        return (newRefreshTokenEntity, newRefreshToken);
    }

    /// <summary>
    /// Revokes an entire token family when token reuse is detected
    /// This is a critical security feature to prevent session hijacking
    /// </summary>
    private async Task RevokeTokenFamilyAsync(RefreshToken compromisedToken)
    {
        _logger.LogWarning("Security Alert: Refresh token reuse detected. Revoking entire token family for User ID: {UserId}", compromisedToken.UserId);
        var tokensToRevoke = new List<RefreshToken>();

        // Find the root of the token family by following the chain backwards
        var currentToken = compromisedToken;
        var visitedHashes = new HashSet<string>();

        // Traverse backwards to find all tokens in the family
        while (currentToken != null && !visitedHashes.Contains(currentToken.TokenHash))
        {
            visitedHashes.Add(currentToken.TokenHash);
            tokensToRevoke.Add(currentToken);

            // Find the token that was replaced by this one (going backwards)
            var previousToken = await _refreshTokenRepository.GetByReplacedTokenHashAsync(currentToken.TokenHash).ConfigureAwait(false);
            currentToken = previousToken;
        }

        // Traverse forwards to find all tokens that replaced this one
        currentToken = compromisedToken;
        while (currentToken?.ReplacedByTokenHash != null && !visitedHashes.Contains(currentToken.ReplacedByTokenHash))
        {
            var nextToken = await _refreshTokenRepository.GetByTokenHashAsync(currentToken.ReplacedByTokenHash).ConfigureAwait(false);
            if (nextToken != null && !visitedHashes.Contains(nextToken.TokenHash))
            {
                visitedHashes.Add(nextToken.TokenHash);
                tokensToRevoke.Add(nextToken);
                currentToken = nextToken;
            }
            else
            {
                break;
            }
        }

        // Revoke all tokens in the family
        foreach (var token in tokensToRevoke)
        {
            if (!token.IsRevoked)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(token).ConfigureAwait(false);
            }
        }
    }
}
