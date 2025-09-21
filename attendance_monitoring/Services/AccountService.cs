using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace attendance_monitoring.Services
{
    public class AccountService(
        IConfiguration configuration,
        ApplicationDbContext context,
        IRefreshTokenService refreshTokenService,
        ILogger<AccountService> logger,
        IAccountRepository accountRepository,
        ISectionRepository sectionRepository
        )
        : IAccountService
    {

        #region Registration Methods
        public async Task<(IdentityResult, RegisterResponseDto?)> RegisterAsync(RegisterDto registerDto)
        {
            logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);

            if (registerDto.Password != registerDto.RepeatedPassword)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Passwords do not match" });
                return (result, null);
            }

            var existingUser = await accountRepository.FindUserByUsernameAsync(registerDto.Username).ConfigureAwait(false);
            if (existingUser != null)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "UsernameExists", Description = "Username already exists" });
                return (result, null);
            }

            existingUser = await accountRepository.FindUserByEmailAsync(registerDto.Email).ConfigureAwait(false);
            if (existingUser != null)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "EmailExists", Description = "Email already exists" });
                return (result, null);
            }

            var user = new IdentityUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var createResult = await accountRepository.CreateUserAsync(user, registerDto.Password).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                return (createResult, null);
            }

            var validRoles = new[] { "Admin", "Teacher", "Student" };
            // Role assignment logic (roles are now ensured to exist at application startup)
            var roleToAssign = "Student";
            if (!string.IsNullOrEmpty(registerDto.Role) && validRoles.Contains(registerDto.Role, StringComparer.OrdinalIgnoreCase))
            {
                roleToAssign = registerDto.Role;
            }

            await accountRepository.AddUserToRoleAsync(user, roleToAssign).ConfigureAwait(false);
            logger.LogInformation("Assigned role {Role} to user {Username}", roleToAssign, user.UserName);

            if (roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                // Validate that the SectionId exists
                var section = await sectionRepository.GetSectionByIdAsync(registerDto.SectionId).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Student registration failed for user {Username}: SectionId {SectionId} does not exist", user.UserName, registerDto.SectionId);
                    var result = IdentityResult.Failed(new IdentityError { Code = "InvalidSection", Description = "The specified section does not exist" });
                    return (result, null);
                }

                var student = new Student
                {
                    Firstname = registerDto.Firstname ?? "",
                    Lastname = registerDto.Lastname ?? "",
                    Email = registerDto.Email,
                    UserId = user.Id,
                    SectionId = registerDto.SectionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await accountRepository.CreateStudentProfileAsync(student).ConfigureAwait(false);
                logger.LogInformation("Created student record for user: {Username}", user.UserName);
            }
            else if (roleToAssign.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                var instructor = new Instructor
                {
                    Firstname = registerDto.Firstname ?? "",
                    Lastname = registerDto.Lastname ?? "",
                    Email = registerDto.Email,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await accountRepository.CreateInstructorProfileAsync(instructor).ConfigureAwait(false);
                logger.LogInformation("Created instructor record for user: {Username}", user.UserName);
            }

            logger.LogInformation("User registered successfully: {Username} with role {Role}", user.UserName, roleToAssign);
            var response = new RegisterResponseDto { Message = $"User registered successfully with {roleToAssign} role" };
            return (IdentityResult.Success, response);
        }
        #endregion

        #region Login Methods
        public async Task<(TokenResponseDto?, string?)> LoginAsync(LoginDto loginDto)
        {
            logger.LogInformation("Login attempt for identifier: {Identifier}", loginDto.Username);

            // Check if the identifier is an email or username
            IdentityUser? user = null;
            if (loginDto.Username.Contains('@'))
            {
                // Treat as email
                user = await accountRepository.FindUserByEmailAsync(loginDto.Username).ConfigureAwait(false);
                if (user != null)
                {
                    logger.LogInformation("Found user by email: {Email}", loginDto.Username);
                }
            }
            else
            {
                // Treat as username
                user = await accountRepository.FindUserByUsernameAsync(loginDto.Username).ConfigureAwait(false);
                if (user != null)
                {
                    logger.LogInformation("Found user by username: {Username}", loginDto.Username);
                }
            }

            if (user == null)
            {
                logger.LogWarning("Login failed for identifier {Identifier}: Invalid email or username or password", loginDto.Username);
                return (null, "Invalid email or username or password");
            }

            var result = await accountRepository.CheckPasswordAsync(user, loginDto.Password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                logger.LogWarning("Login failed for user {Username}: Invalid password", user.UserName);
                return (null, "Invalid email or username or password");
            }

            var accessToken = await GenerateJwtToken(user).ConfigureAwait(false);
            var (refreshTokenEntity, refreshToken) = await refreshTokenService.CreateRefreshTokenAsync(user.Id).ConfigureAwait(false);

            logger.LogInformation("User {Username} logged in successfully", user.UserName);
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return (tokenResponse, null);
        }
        #endregion

        #region Token Management Methods
        public async Task<(TokenResponseDto?, string?)> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest)
        {
            logger.LogInformation("Token refresh attempt.");

            var (refreshTokenEntity, validationError) = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken).ConfigureAwait(false);
            if (refreshTokenEntity == null)
            {
                logger.LogWarning("Token refresh failed: {ValidationError}", validationError);
                return (null, validationError);
            }

            var user = await accountRepository.FindUserByIdAsync(refreshTokenEntity.UserId).ConfigureAwait(false);
            if (user == null)
            {
                logger.LogWarning("Token refresh failed: User not found for token.");
                return (null, "User not found");
            }

            // Blacklist the old access token if provided
            if (!string.IsNullOrEmpty(refreshTokenRequest.OldAccessToken))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    
                    // Clone TokenValidationParameters from our JwtBearer configuration
                    var issuer = configuration["AppSettings:Issuer"];
                    var audience = configuration["AppSettings:Audience"];
                    var tokenKey = configuration["AppSettings:Token"];
                    
                    if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(tokenKey))
                    {
                        logger.LogWarning("Token validation failed: Missing configuration values for issuer, audience, or token key.");
                        throw new InvalidOperationException("Token validation configuration is incomplete.");
                    }
                    
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
                    var claimsPrincipal = tokenHandler.ValidateToken(refreshTokenRequest.OldAccessToken, tokenValidationParameters, out var validatedToken);
                    
                    // Extract jti and ValidTo only after successful validation
                    var jti = claimsPrincipal.FindFirst("jti")?.Value;
                    var expiresAt = validatedToken.ValidTo;
                    var sub = claimsPrincipal.FindFirst("sub")?.Value;

                    // Only blacklist if all validation checks pass:
                    // 1. Token is issued by us (signature valid, issuer/audience match) - already validated by ValidateToken
                    // 2. Subject (sub) matches the user from the refresh token
                    // 3. It has not expired yet
                    // 4. jti is not null or empty
                    
                    // Testing
                    // if (!string.IsNullOrEmpty(jti) && sub == user.Id && expiresAt > DateTime.UtcNow)
                    // {
                    //     await BlacklistTokenAsync(jti, expiresAt);
                    //     logger.LogInformation("Old access token blacklisted for user {UserId}.");
                    // }
                    // else if (!string.IsNullOrEmpty(jti))
                    // {
                    //     logger.LogWarning("Old access token not blacklisted - validation failed for user {UserId}.");
                    // }
                    // else if (jti == null)
                    // {
                    //     logger.LogWarning("Old access token not blacklisted - token has no JTI for user {UserId}.");
                    // }
                    
                    // Simplified switch expression for better readability
                    switch (jti)
                    {
                        case not null when sub == user.Id && expiresAt > DateTime.UtcNow:
                            await BlacklistTokenAsync(jti, expiresAt).ConfigureAwait(false);
                            logger.LogInformation("Old access token blacklisted for user {UserId}.", user.Id);
                            break;

                        case not null:
                            logger.LogWarning("Old access token not blacklisted - validation failed for user {UserId}.", user.Id);
                            break;

                        default: // case null
                            logger.LogWarning("Old access token not blacklisted - token has no JTI for user {UserId}.", user.Id);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogWarning("Failed to validate and blacklist old access token: {Error}", ex.Message);
                }
            }

            var (newRefreshTokenEntity, newRefreshToken) = await refreshTokenService.RotateRefreshTokenAsync(
                refreshTokenRequest.RefreshToken,
                user.Id).ConfigureAwait(false);

            if (string.IsNullOrEmpty(newRefreshToken))
            {
                logger.LogError("Token refresh failed for user {UserId}: Failed to rotate refresh token.", user.Id);
                return (null, "Failed to rotate refresh token");
            }

            var newAccessToken = await GenerateJwtToken(user).ConfigureAwait(false);

            logger.LogInformation("Token refreshed successfully for user {UserId}.", user.Id);
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
            return (tokenResponse, null);
        }

        public async Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
        {
            logger.LogInformation("Token revocation attempt for user {UserId}.", userId);

            var tokenHash = refreshTokenService.HashRefreshToken(revokeTokenRequest.RefreshToken);
            var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);

            if (storedToken == null)
            {
                logger.LogWarning("Token revocation failed: Refresh token not found.");
                return (null, "Refresh token not found");
            }

            if (storedToken.UserId != userId)
            {
                logger.LogWarning("Token revocation failed: Refresh token does not belong to the current user {UserId}.", userId);
                return (null, "Refresh token does not belong to the current user");
            }

            if (storedToken.IsRevoked)
            {
                logger.LogWarning("Token revocation failed: Refresh token has already been revoked.");
                return (null, "Refresh token has already been revoked");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("Token revocation failed: Refresh token has expired.");
                return (null, "Refresh token has expired");
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            var response = new RevokeResponseDto { Message = "Refresh token revoked successfully" };
            return (response, null);
        }
        #endregion

        #region Helper Methods
        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            var roles = await accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
            // Use LINQ Select expression instead of FOREACH to transform each role string into a Claim object with the Role claim type,
            // then add all of these Claim objects to the claims collection at once using AddRange 
            // for improved readability and better performance than adding items individually in a loop.
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenKey = configuration["AppSettings:Token"] ?? string.Empty;
            if (string.IsNullOrEmpty(tokenKey))
            {
                throw new InvalidOperationException("Token key is not configured properly in AppSettings.");
            }
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["AppSettings:Issuer"],
                audience: configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(TokenConstants.AccessTokenExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        /// <summary>
        /// Blacklists a JWT token by its JTI
        /// </summary>
        /// <param name="jti">The JTI of the token to blacklist</param>
        /// <param name="expiresAt">The expiration time of the token</param>
        private async Task BlacklistTokenAsync(string jti, DateTime expiresAt)
        {
            var blacklistedToken = new BlacklistedToken
            {
                Jti = jti,
                BlacklistedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };
            
            try
            {
                context.BlacklistedTokens.Add(blacklistedToken);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                // Token is already blacklisted, treat as idempotent operation
                // This can happen if the same token is blacklisted multiple times
                // We simply ignore the exception and continue
            }
        }
        #endregion
    }
}
