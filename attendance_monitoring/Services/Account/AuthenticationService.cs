using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace attendance_monitoring.Services.Account;

/// <summary>
/// Focused unit responsible for authentication and token management.
/// Handles login, token refresh/revoke/logout, JWT generation, and token blacklisting.
/// </summary>
internal sealed class AuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IConfiguration configuration,
        ApplicationDbContext context,
        IRefreshTokenService refreshTokenService,
        IAccountRepository accountRepository,
        ILogger<AuthenticationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user and returns access tokens.
    /// </summary>
    /// <param name="loginDto">The login credentials.</param>
    /// <returns>The login result containing tokens and user info.</returns>
    /// <exception cref="ValidationException">Thrown when credentials are invalid.</exception>
    public async Task<LoginResult> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for identifier: {Identifier}", loginDto.Username);

        // Check if the identifier is an email or username
        IdentityUser? user;
        if (loginDto.Username.Contains('@'))
        {
            // Treat as email
            user = await _accountRepository.FindUserByEmailAsync(loginDto.Username).ConfigureAwait(false);
            if (user != null)
            {
                _logger.LogInformation("Found user by email: {Email}", loginDto.Username);
            }
        }
        else
        {
            // Treat as username
            user = await _accountRepository.FindUserByUsernameAsync(loginDto.Username).ConfigureAwait(false);
            if (user != null)
            {
                _logger.LogInformation("Found user by username: {Username}", loginDto.Username);
            }
        }

        if (user == null)
        {
            _logger.LogWarning("Login failed for identifier {Identifier}: Invalid email or username or password", loginDto.Username);
            throw new ValidationException("Invalid email or username or password");
        }

        var result = await _accountRepository.CheckPasswordAsync(user, loginDto.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for user {Username}: Invalid password", user.UserName);
            throw new ValidationException("Invalid email or username or password");
        }

        var roles = await _accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
        var role = roles?.FirstOrDefault();
        if (string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("Login failed: User {Username} (ID: {UserId}) has no assigned roles.", user.UserName, user.Id);
            throw new ValidationException("User has no assigned roles and cannot be authenticated.");
        }

        var accessToken = await GenerateJwtToken(user).ConfigureAwait(false);
        var (_, refreshToken) = await _refreshTokenService.CreateRefreshTokenAsync(user.Id).ConfigureAwait(false);

        _logger.LogInformation("User {Username} logged in successfully", user.UserName);
        return new LoginResult
        {
            TokenResponse = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            Username = user.UserName ?? string.Empty,
            Role = role
        };
    }

    /// <summary>
    /// Refreshes access tokens using a refresh token.
    /// </summary>
    /// <param name="refreshTokenRequest">The refresh token request.</param>
    /// <returns>The new token response.</returns>
    /// <exception cref="ValidationException">Thrown when the refresh token is invalid.</exception>
    /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found.</exception>
    /// <exception cref="EntityServiceException">Thrown when token rotation fails.</exception>
    public async Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest)
    {
        _logger.LogInformation("Token refresh attempt.");

        // ValidateRefreshTokenAsync now throws ValidationException if invalid
        var refreshTokenEntity = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken).ConfigureAwait(false);

        var user = await _accountRepository.FindUserByIdAsync(refreshTokenEntity.UserId).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Token refresh failed: User not found for token.");
            throw new EntityNotFoundException<string>("User", refreshTokenEntity.UserId, "User not found");
        }

        // Blacklist the old access token if provided
        if (!string.IsNullOrEmpty(refreshTokenRequest.OldAccessToken))
        {
            await ValidateAndBlacklistTokenAsync(refreshTokenRequest.OldAccessToken, user.Id, "token refresh");
        }

        // RotateRefreshTokenAsync now throws exceptions on failure
        var (_, newRefreshToken) = await _refreshTokenService.RotateRefreshTokenAsync(
            refreshTokenRequest.RefreshToken,
            user.Id).ConfigureAwait(false);

        var newAccessToken = await GenerateJwtToken(user).ConfigureAwait(false);

        _logger.LogInformation("Token refreshed successfully for user {UserId}.", user.Id);
        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="revokeTokenRequest">The revoke token request.</param>
    /// <param name="userId">The user ID performing the revocation.</param>
    /// <returns>The revoke response.</returns>
    /// <exception cref="ValidationException">Thrown when the token is invalid or cannot be revoked.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when the token doesn't belong to the user.</exception>
    /// <exception cref="EntityServiceException">Thrown when revocation fails due to database errors.</exception>
    public async Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
    {
        _logger.LogInformation("Token revocation attempt for user {UserId}.", userId);

        try
        {
            var tokenHash = _refreshTokenService.HashRefreshToken(revokeTokenRequest.RefreshToken);
            var storedToken = await _accountRepository.FindRefreshTokenByHashAsync(tokenHash).ConfigureAwait(false);

            if (storedToken == null)
            {
                _logger.LogWarning("Token revocation failed: Refresh token not found.");
                throw new ValidationException("Refresh token not found");
            }

            if (storedToken.UserId != userId)
            {
                _logger.LogWarning("Token revocation failed: Refresh token does not belong to the current user {UserId}.", userId);
                throw new EntityUnauthorizedException("RefreshToken", "Revoke", userId, "Refresh token does not belong to the current user");
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Token revocation failed: Refresh token has already been revoked.");
                throw new ValidationException("Refresh token has already been revoked");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token revocation failed: Refresh token has expired.");
                throw new ValidationException("Refresh token has expired");
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _accountRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            return new RevokeResponseDto { Message = "Refresh token revoked successfully" };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Token revocation concurrency issue for user {UserId}", userId);
            throw new EntityServiceException("RefreshToken", "Revoke", "Token revocation failed due to a concurrency issue", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Token revocation database update failed for user {UserId}", userId);
            throw new EntityServiceException("RefreshToken", "Revoke", "Token revocation failed due to a database error", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token revocation operation failed for user {UserId}", userId);
            throw ExceptionHandlingHelper.CreateServiceException("RefreshToken", "Revoke", ex);
        }
    }

    /// <summary>
    /// Logs out a user by revoking all tokens.
    /// </summary>
    public async Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken)
    {
        _logger.LogInformation("Logout attempt for user {UserId}.", userId);
        await RevokeAllTokensAsync(userId, accessToken, "logout");
        _logger.LogInformation("User logged out successfully: {UserId}", userId);
        return new LogoutResponseDto { Success = true, Message = "Logged out successfully" };
    }

    /// <summary>
    /// Logs out a web user by revoking all tokens.
    /// </summary>
    public async Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken)
    {
        _logger.LogInformation("Web logout attempt for user {UserId}.", userId);
        await RevokeAllTokensAsync(userId, accessToken, "web logout");
        _logger.LogInformation("User web logout completed successfully: {UserId}", userId);
        return new LogoutResponseDto { Success = true, Message = "Logged out successfully" };
    }

    /// <summary>
    /// Blacklists a JWT token by its JTI.
    /// </summary>
    public async Task BlacklistTokenAsync(string jti, DateTime expiresAt)
    {
        var blacklistedToken = new BlacklistedToken
        {
            Jti = jti,
            BlacklistedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        try
        {
            _context.BlacklistedTokens.Add(blacklistedToken);
            await _accountRepository.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency issue while blacklisting token {Jti}.", jti);
        }
        catch (DbUpdateException ex)
        {
            // Token is already blacklisted or other DB update issue; treat duplicate as idempotent
            _logger.LogWarning(ex, "Blacklisting token {Jti} may have already occurred. Treating as idempotent.", jti);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Revokes all active tokens for a user during logout operations.
    /// </summary>
    private async Task RevokeAllTokensAsync(string userId, string? accessToken, string operationType)
    {
        try
        {
            // Blacklist the access token if provided
            if (!string.IsNullOrEmpty(accessToken))
            {
                await ValidateAndBlacklistTokenAsync(accessToken, userId, operationType);
            }

            // Revoke all active refresh tokens for the user
            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync().ConfigureAwait(false);

            foreach (var token in activeRefreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            if (activeRefreshTokens.Count > 0)
            {
                await _accountRepository.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Revoked {TokenCount} active refresh tokens during {OperationType} for user {UserId}.",
                    activeRefreshTokens.Count, operationType, userId);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "{OperationType} concurrency issue for user {UserId}", operationType, userId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "{OperationType} database update failed for user {UserId}", operationType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationType} operation failed for user {UserId}", operationType, userId);
        }
    }

    /// <summary>
    /// Validates and blacklists an access token if it's valid and belongs to the specified user.
    /// </summary>
    private async Task ValidateAndBlacklistTokenAsync(string accessToken, string userId, string operationType)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Use validated configuration values
            var issuer = JwtConfigurationValidator.GetValidatedIssuer(_configuration);
            var audience = JwtConfigurationValidator.GetValidatedAudience(_configuration);
            var tokenKey = JwtConfigurationValidator.GetValidatedToken(_configuration);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey))
            };

            // Validate the token
            var claimsPrincipal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);

            var jti = claimsPrincipal.FindFirst("jti")?.Value;
            var expiresAt = validatedToken.ValidTo;
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Only blacklist if token is valid, has JTI, belongs to current user, and hasn't expired
            if (!string.IsNullOrEmpty(jti) && userIdClaim == userId && expiresAt > DateTime.UtcNow)
            {
                await BlacklistTokenAsync(jti, expiresAt).ConfigureAwait(false);
                _logger.LogInformation("Access token blacklisted during {OperationType} for user {UserId}", operationType, userId);
            }
            else
            {
                _logger.LogDebug("Access token not blacklisted during {OperationType} - validation checks failed for user {UserId}", operationType, userId);
            }
        }
        catch (SecurityTokenExpiredException ex)
        {
            // Expected case: Token has already expired, no need to blacklist
            _logger.LogDebug("Token already expired during {OperationType} for user {UserId}: {Message}",
                operationType, userId, ex.Message);
        }
        catch (SecurityTokenValidationException ex)
        {
            // Token itself is invalid - this is sometimes expected (e.g., malformed, wrong signature)
            _logger.LogInformation("Token validation failed during {OperationType} for user {UserId}: {Message}",
                operationType, userId, ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Concurrency issue - token may already be blacklisted (catch before DbUpdateException)
            _logger.LogWarning(ex, "Concurrency issue during token blacklist for {OperationType}: {UserId}",
                operationType, userId);
        }
        catch (DbUpdateException ex)
        {
            // Database error during blacklisting - this is critical
            _logger.LogError(ex, "CRITICAL: Failed to blacklist token during {OperationType} for user {UserId}. Token may remain active.",
                operationType, userId);
        }
        catch (Exception ex)
        {
            // Unexpected error - potential security issue
            _logger.LogError(ex, "Unexpected error during token blacklist for {OperationType}: {UserId}. Token may remain active.",
                operationType, userId);
        }
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        var roles = await _accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Use validated token key
        var tokenKey = JwtConfigurationValidator.GetValidatedToken(_configuration);
        var issuer = JwtConfigurationValidator.GetValidatedIssuer(_configuration);
        var audience = JwtConfigurationValidator.GetValidatedAudience(_configuration);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TokenConstants.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
