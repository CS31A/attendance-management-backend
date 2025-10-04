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

public class RefreshTokenService(
    IRefreshTokenRepository refreshTokenRepository,
    UserManager<IdentityUser> userManager,
    ILogger<RefreshTokenService> logger)
    : IRefreshTokenService
{
    
    private readonly UserManager<IdentityUser> _userManager = userManager;

    #region Token Generation Methods

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

        try
        {
            await refreshTokenRepository.CreateAsync(refreshTokenEntity).ConfigureAwait(false);
            await refreshTokenRepository.SaveChangesAsync().ConfigureAwait(false);

            return (refreshTokenEntity, refreshToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating refresh token for user ID: {UserId}", userId);
            throw; // Re-throw the exception to maintain the existing behavior while logging it
        }
    }

    #endregion

    #region Token Validation Methods

    public async Task<(RefreshToken?, string?)> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash).ConfigureAwait(false);

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

    #endregion

    #region Token Management Methods

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string userId)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash).ConfigureAwait(false);

        if (storedToken == null || storedToken.UserId != userId) return false;
        
        try
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await refreshTokenRepository.UpdateAsync(storedToken).ConfigureAwait(false);
            await refreshTokenRepository.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while revoking refresh token for user ID: {UserId}", userId);
            return false;
        }
    }

    public async Task<(RefreshToken?, string?)> RotateRefreshTokenAsync(string oldRefreshToken, string userId)
    {
        // Validate the old token first
        var (storedToken, validationError) = await ValidateRefreshTokenAsync(oldRefreshToken).ConfigureAwait(false);

        if (storedToken == null)
        {
            return (null, validationError);
        }

        try
        {
            // Mark the old token as revoked
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Create a new refresh token
            var (newRefreshTokenEntity, newRefreshToken) = await CreateRefreshTokenAsync(userId).ConfigureAwait(false);

            // Link the old token to the new one
            storedToken.ReplacedByTokenHash = newRefreshTokenEntity.TokenHash;

            // Update the old token
            await refreshTokenRepository.UpdateAsync(storedToken).ConfigureAwait(false);
            await refreshTokenRepository.SaveChangesAsync().ConfigureAwait(false);

            return (newRefreshTokenEntity, newRefreshToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while rotating refresh token for user ID: {UserId}", userId);
            return (null, "An error occurred while rotating the refresh token. Please try again later.");
        }
    }

    #endregion

    #region Security Methods

    /// <summary>
    /// Revokes an entire token family when token reuse is detected
    /// This is a critical security feature to prevent session hijacking
    /// </summary>
    private async Task RevokeTokenFamilyAsync(RefreshToken compromisedToken)
    {
        logger.LogWarning("Security Alert: Refresh token reuse detected. Revoking entire token family for User ID: {UserId}", compromisedToken.UserId);
        var tokensToRevoke = new List<RefreshToken>();

        try
        {
            // Find the root of the token family by following the chain backwards
            var currentToken = compromisedToken;
            var visitedHashes = new HashSet<string>();

            // Traverse backwards to find all tokens in the family
            while (currentToken != null && !visitedHashes.Contains(currentToken.TokenHash))
            {
                visitedHashes.Add(currentToken.TokenHash);
                tokensToRevoke.Add(currentToken);

                // Find the token that was replaced by this one (going backwards)
                var previousToken = await refreshTokenRepository.GetByReplacedTokenHashAsync(currentToken.TokenHash).ConfigureAwait(false);
                currentToken = previousToken;
            }

            // Traverse forwards to find all tokens that replaced this one
            currentToken = compromisedToken;
            while (currentToken?.ReplacedByTokenHash != null && !visitedHashes.Contains(currentToken.ReplacedByTokenHash))
            {
                var nextToken = await refreshTokenRepository.GetByTokenHashAsync(currentToken.ReplacedByTokenHash).ConfigureAwait(false);
                if (nextToken != null && visitedHashes.Add(nextToken.TokenHash))
                {
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
                if (token.IsRevoked) continue;
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await refreshTokenRepository.UpdateAsync(token).ConfigureAwait(false);
                await refreshTokenRepository.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while revoking token family for user ID: {UserId}", compromisedToken.UserId);
            throw; // Re-throw to maintain the existing behavior while logging it
        }
    }

    #endregion
}
