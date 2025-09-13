using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace attendance_monitoring.Services
{
    public class AccountService(
        IConfiguration configuration,
        ApplicationDbContext context,
        IRefreshTokenService refreshTokenService,
        ILogger<AccountService> logger,
        IAccountRepository accountRepository)
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

            var existingUser = await accountRepository.FindUserByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "UsernameExists", Description = "Username already exists" });
                return (result, null);
            }

            existingUser = await accountRepository.FindUserByEmailAsync(registerDto.Email);
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

            var createResult = await accountRepository.CreateUserAsync(user, registerDto.Password);
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

            await accountRepository.AddUserToRoleAsync(user, roleToAssign);
            logger.LogInformation("Assigned role {Role} to user {Username}", roleToAssign, user.UserName);

            if (roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                var student = new Student
                {
                    Firstname = registerDto.Firstname ?? "",
                    Lastname = registerDto.Lastname ?? "",
                    Email = registerDto.Email,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await accountRepository.CreateStudentProfileAsync(student);
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
                await accountRepository.CreateInstructorProfileAsync(instructor);
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
            if (loginDto.Username.Contains("@"))
            {
                // Treat as email
                user = await accountRepository.FindUserByEmailAsync(loginDto.Username);
                if (user != null)
                {
                    logger.LogInformation("Found user by email: {Email}", loginDto.Username);
                }
            }
            else
            {
                // Treat as username
                user = await accountRepository.FindUserByUsernameAsync(loginDto.Username);
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

            var result = await accountRepository.CheckPasswordAsync(user, loginDto.Password);
            if (!result.Succeeded)
            {
                logger.LogWarning("Login failed for user {Username}: Invalid password", user.UserName);
                return (null, "Invalid email or username or password");
            }

            var accessToken = await GenerateJwtToken(user);
            var (refreshTokenEntity, refreshToken) = await refreshTokenService.CreateRefreshTokenAsync(user.Id);

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

            var (refreshTokenEntity, validationError) = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
            if (refreshTokenEntity == null)
            {
                logger.LogWarning("Token refresh failed: {ValidationError}", validationError);
                return (null, validationError);
            }

            var user = await accountRepository.FindUserByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                logger.LogWarning("Token refresh failed: User not found for token.");
                return (null, "User not found");
            }

            var (newRefreshTokenEntity, newRefreshToken) = await refreshTokenService.RotateRefreshTokenAsync(
                refreshTokenRequest.RefreshToken,
                user.Id);

            if (string.IsNullOrEmpty(newRefreshToken))
            {
                logger.LogError("Token refresh failed for user {UserId}: Failed to rotate refresh token.", user.Id);
                return (null, "Failed to rotate refresh token");
            }

            var newAccessToken = await GenerateJwtToken(user);

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
            var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

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
            await context.SaveChangesAsync();

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

            var roles = await accountRepository.GetUserRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["AppSettings:Token"] ?? string.Empty));
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
    }
}
