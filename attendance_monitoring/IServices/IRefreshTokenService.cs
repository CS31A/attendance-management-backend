using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for managing refresh tokens for authentication.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token string.
    /// </summary>
    /// <returns>A base64-encoded refresh token string.</returns>
    Task<string> GenerateRefreshTokenAsync();

    /// <summary>
    /// Hashes a refresh token for secure storage.
    /// </summary>
    /// <param name="refreshToken">The refresh token to hash.</param>
    /// <returns>The hashed refresh token.</returns>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Creates and stores a new refresh token for a user.
    /// </summary>
    /// <param name="userId">The user ID to create the token for.</param>
    /// <returns>A tuple containing the refresh token entity and the raw token string.</returns>
    /// <exception cref="EntityServiceException">Thrown when token creation fails.</exception>
    Task<(RefreshToken, string)> CreateRefreshTokenAsync(string userId);

    /// <summary>
    /// Validates a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>The validated refresh token entity.</returns>
    /// <exception cref="ValidationException">Thrown when the token is invalid, expired, or revoked.</exception>
    Task<RefreshToken> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes a refresh token for a user.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="userId">The user ID that owns the token.</param>
    /// <returns>True if the token was successfully revoked.</returns>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string userId);

    /// <summary>
    /// Rotates a refresh token by revoking the old one and creating a new one.
    /// </summary>
    /// <param name="oldRefreshToken">The old refresh token to rotate.</param>
    /// <param name="userId">The user ID that owns the token.</param>
    /// <returns>A tuple containing the new refresh token entity and the raw token string.</returns>
    /// <exception cref="ValidationException">Thrown when the old token is invalid.</exception>
    /// <exception cref="EntityServiceException">Thrown when rotation fails.</exception>
    Task<(RefreshToken, string)> RotateRefreshTokenAsync(string oldRefreshToken, string userId);
}