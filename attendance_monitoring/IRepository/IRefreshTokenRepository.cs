using attendance_monitoring.Classes;
using System;
using System.Threading.Tasks;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by its hash.
    /// </summary>
    /// <param name="tokenHash">The hash of the refresh token.</param>
    /// <returns>The refresh token if found; otherwise, null.</returns>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Retrieves a refresh token by its replaced token hash.
    /// </summary>
    /// <param name="replacedTokenHash">The hash of the replaced refresh token.</param>
    /// <returns>The refresh token if found; otherwise, null.</returns>
    Task<RefreshToken?> GetByReplacedTokenHashAsync(string replacedTokenHash);

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to create.</param>
    /// <returns>The created refresh token.</returns>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Updates an existing refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Checks if a refresh token exists by its hash.
    /// </summary>
    /// <param name="tokenHash">The hash of the refresh token.</param>
    /// <returns>True if the refresh token exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string tokenHash);
}