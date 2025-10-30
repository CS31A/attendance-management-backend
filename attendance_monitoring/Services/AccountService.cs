using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
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
        ISectionRepository sectionRepository,
        IUserFactory userFactory,
        IInstructorRepository instructorRepository
        )
        : IAccountService
    {

        #region Registration Methods
        #region RegisterAsync
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

            // Map Instructor to Teacher (Instructor is an alias for Teacher)
            var requestedRole = registerDto.Role;
            if (!string.IsNullOrEmpty(requestedRole) && requestedRole.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            {
                requestedRole = "Teacher";
            }

            var validRoles = new[] { "Admin", "Teacher", "Student" };
            // Role assignment logic (roles are now ensured to exist at application startup)
            var roleToAssign = "Student";
            if (!string.IsNullOrEmpty(requestedRole) && validRoles.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
            {
                roleToAssign = requestedRole;
            }

            // For students, validate that the SectionId exists before attempting user creation
            if (roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                // Check if SectionId is provided for students
                if (!registerDto.SectionId.HasValue)
                {
                    logger.LogWarning("Student registration failed for username {Username}: SectionId is required for students", registerDto.Username);
                    var result = IdentityResult.Failed(new IdentityError { Code = "InvalidSection", Description = "SectionId is required for student registration" });
                    return (result, null);
                }

                var section = await sectionRepository.GetSectionByIdAsync(registerDto.SectionId.Value).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Student registration failed for username {Username}: SectionId {SectionId} does not exist", registerDto.Username, registerDto.SectionId);
                    var result = IdentityResult.Failed(new IdentityError { Code = "InvalidSection", Description = "The specified section does not exist" });
                    return (result, null);
                }
            }

            // Use UserFactory to create the user with appropriate role and profile
            var userCreationResult = await userFactory.CreateUserAsync(
                registerDto.Username,
                registerDto.Email,
                registerDto.Password,
                roleToAssign,
                registerDto.Firstname,
                registerDto.Lastname,
                roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase) ? registerDto.SectionId : null
            ).ConfigureAwait(false);

            if (!userCreationResult.Success)
            {
                var errors = userCreationResult.Errors.Select(error => new IdentityError { Description = error }).ToArray();
                var result = IdentityResult.Failed(errors);
                logger.LogWarning("User registration failed for username {Username}: {Errors}", registerDto.Username, string.Join(", ", userCreationResult.Errors));
                return (result, null);
            }

            logger.LogInformation("User registered successfully: {Username} with role {Role}", registerDto.Username, roleToAssign);
            var response = new RegisterResponseDto { Message = $"User registered successfully with {roleToAssign} role" };
            return (IdentityResult.Success, response);
        }
        #endregion
        #endregion

        #region Login Methods
        #region LoginAsync
        public async Task<(TokenResponseDto?, string?, string?)> LoginAsync(LoginDto loginDto)
        {
            logger.LogInformation("Login attempt for identifier: {Identifier}", loginDto.Username);

            // Check if the identifier is an email or username
            IdentityUser? user;
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
                return (null, null, "Invalid email or username or password");
            }

            var result = await accountRepository.CheckPasswordAsync(user, loginDto.Password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                logger.LogWarning("Login failed for user {Username}: Invalid password", user.UserName);
                return (null, null, "Invalid email or username or password");
            }

            var accessToken = await GenerateJwtToken(user).ConfigureAwait(false);
            var (_, refreshToken) = await refreshTokenService.CreateRefreshTokenAsync(user.Id).ConfigureAwait(false);

            logger.LogInformation("User {Username} logged in successfully", user.UserName);
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return (tokenResponse, user.UserName, null);
        }
        #endregion
        #endregion

        #region Token Management Methods
        #region RefreshAsync
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
                await ValidateAndBlacklistTokenAsync(refreshTokenRequest.OldAccessToken, user.Id, "token refresh");
            }

            var (_, newRefreshToken) = await refreshTokenService.RotateRefreshTokenAsync(
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
        #endregion

        #region RevokeAsync
        public async Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
        {
            logger.LogInformation("Token revocation attempt for user {UserId}.", userId);

            try
            {
                var tokenHash = refreshTokenService.HashRefreshToken(revokeTokenRequest.RefreshToken);
                var storedToken = await accountRepository.FindRefreshTokenByHashAsync(tokenHash).ConfigureAwait(false);

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
                await accountRepository.SaveChangesAsync().ConfigureAwait(false);

                logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
                var response = new RevokeResponseDto { Message = "Refresh token revoked successfully" };
                return (response, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Token revocation concurrency issue for user {UserId}", userId);
                return (null, "Token revocation failed due to a concurrency issue");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Token revocation database update failed for user {UserId}", userId);
                return (null, "Token revocation failed due to a database error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token revocation operation failed for user {UserId}", userId);
                return (null, "Token revocation failed due to an unexpected error");
            }
        }
        #endregion

        #region LogoutAsync
        public async Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken)
        {
            logger.LogInformation("Logout attempt for user {UserId}.", userId);

            try
            {
                // Blacklist the access token if provided
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await ValidateAndBlacklistTokenAsync(accessToken, userId, "logout");
                }

                // Revoke all active refresh tokens for the user
                var activeRefreshTokens = await context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync().ConfigureAwait(false);

                foreach (var token in activeRefreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                if (activeRefreshTokens.Count > 0)
                {
                    await accountRepository.SaveChangesAsync().ConfigureAwait(false);
                    logger.LogInformation("Revoked {TokenCount} active refresh tokens during logout for user {UserId}.", activeRefreshTokens.Count, userId);
                }

                logger.LogInformation("User logged out successfully: {UserId}", userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Logout concurrency issue for user {UserId}", userId);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Logout database update failed for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Logout operation failed for user {UserId}", userId);
            }

            return new LogoutResponseDto { Success = true, Message = "Logged out successfully" };
        }
        #endregion

        #region WebLogoutAsync
        public async Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken)
        {
            logger.LogInformation("Web logout attempt for user {UserId}.", userId);

            try
            {
                // Blacklist the access token if provided (same logic as regular logout)
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await ValidateAndBlacklistTokenAsync(accessToken, userId, "web logout");
                }

                // Revoke all active refresh tokens for the user (consistent with JWT logout)
                var activeRefreshTokens = await context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync().ConfigureAwait(false);

                foreach (var token in activeRefreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                if (activeRefreshTokens.Count > 0)
                {
                    await accountRepository.SaveChangesAsync().ConfigureAwait(false);
                    logger.LogInformation("Revoked {TokenCount} active refresh tokens during web logout for user {UserId}.", activeRefreshTokens.Count, userId);
                }

                logger.LogInformation("User web logout completed successfully: {UserId}", userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Web logout concurrency issue for user {UserId}", userId);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Web logout database update failed for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Web logout operation failed for user {UserId}", userId);
            }

            return new LogoutResponseDto { Success = true, Message = "Logged out successfully" };
        }
        #endregion
        #endregion

        #region Helper Methods

        #region ValidateAndBlacklistTokenAsync
        /// <summary>
        /// Validates and blacklists an access token if it's valid and belongs to the specified user
        /// </summary>
        /// <param name="accessToken">The access token to validate and blacklist</param>
        /// <param name="userId">The user ID that should own the token</param>
        /// <param name="operationType">Type of operation for logging purposes</param>
        private async Task ValidateAndBlacklistTokenAsync(string accessToken, string userId, string operationType)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

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
                var claimsPrincipal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);

                var jti = claimsPrincipal.FindFirst("jti")?.Value;
                var expiresAt = validatedToken.ValidTo;
                var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Only blacklist if token is valid, has JTI, belongs to current user, and hasn't expired
                if (!string.IsNullOrEmpty(jti) && userIdClaim == userId && expiresAt > DateTime.UtcNow)
                {
                    await BlacklistTokenAsync(jti, expiresAt).ConfigureAwait(false);
                    logger.LogInformation("Access token blacklisted during {OperationType} for user {UserId}", operationType, userId);
                }
                else
                    logger.LogWarning("Access token not blacklisted during {OperationType} - validation failed for user {UserId}", operationType, userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to validate and blacklist access token during {OperationType} for user {UserId}: {Error}", operationType, userId, ex.Message);
                // Continue with operation even if blacklisting fails
            }
        }
        #endregion

        #region GenerateJwtToken
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
        #endregion

        #region BlacklistTokenAsync
        /// <summary>
        /// Blacklists a JWT token by its JTI
        /// </summary>
        /// <param name="jti">The JTI of the token to blacklist</param>
        /// <param name="expiresAt">The expiration time of the token</param>
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
                context.BlacklistedTokens.Add(blacklistedToken);
                await accountRepository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency issue while blacklisting token {Jti}.", jti);
            }
            catch (DbUpdateException ex)
            {
                // Token is already blacklisted or other DB update issue; treat duplicate as idempotent
                logger.LogWarning(ex, "Blacklisting token {Jti} may have already occurred. Treating as idempotent.", jti);
            }
        }
        #endregion

        #region GetUserProfileAsync
        /// <summary>
        /// Gets comprehensive user profile information including role-specific data
        /// </summary>
        /// <param name="userId">The user ID to retrieve profile for</param>
        /// <returns>User profile DTO with role-specific information</returns>
        public async Task<(UserProfileResponseDto?, string?)> GetUserProfileAsync(string userId)
        {
            logger.LogInformation("Fetching user profile for user ID: {UserId}", userId);

            // Get the user from Identity
            var user = await accountRepository.FindUserByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                logger.LogWarning("User profile fetch failed: User not found for ID {UserId}", userId);
                return (null, "User not found");
            }

            // Get user roles
            var roles = await accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
            var role = roles.FirstOrDefault() ?? "Student"; // Default to Student if no role found

            // Build base profile
            var profile = new UserProfileResponseDto
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), // Will be overridden by role-specific data if available
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            // Fetch role-specific data
            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                var student = await context.Students
                    .Include(s => s.User)
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec.Course)
                    .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted)
                    .ConfigureAwait(false);

                if (student != null)
                {
                    profile.StudentProfile = new StudentProfileInfo
                    {
                        Id = student.Id,
                        Firstname = student.Firstname,
                        Lastname = student.Lastname,
                        IsRegular = student.IsRegular,
                        SectionId = student.SectionId,
                        SectionName = student.Section?.Name ?? string.Empty,
                        CourseId = student.Section?.CourseId ?? 0,
                        CourseName = student.Section?.Course?.Name ?? string.Empty,
                        CreatedAt = student.CreatedAt,
                        UpdatedAt = student.UpdatedAt
                    };
                    profile.CreatedAt = student.CreatedAt;
                    profile.UpdatedAt = student.UpdatedAt;
                }
            }
            else if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || role.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            {
                var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor != null)
                {
                    profile.InstructorProfile = new InstructorProfileInfo
                    {
                        Id = instructor.Id,
                        Firstname = instructor.Firstname,
                        Lastname = instructor.Lastname,
                        CreatedAt = instructor.CreatedAt,
                        UpdatedAt = instructor.UpdatedAt
                    };
                    profile.CreatedAt = instructor.CreatedAt;
                    profile.UpdatedAt = instructor.UpdatedAt;
                }
            }
            // Admin role: only base Identity information (already populated)

            logger.LogInformation("User profile fetched successfully for user ID: {UserId}", userId);
            return (profile, null);
        }
        #endregion
        #endregion
    }
}
