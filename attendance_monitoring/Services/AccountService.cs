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
    public class AccountService : IAccountService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AccountService> _logger;
        private readonly IAccountRepository _accountRepository;

        public AccountService(
            IConfiguration configuration,
            ApplicationDbContext context,
            IRefreshTokenService refreshTokenService,
            ILogger<AccountService> logger,
            IAccountRepository accountRepository)
        {
            _configuration = configuration;
            _context = context;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
            _accountRepository = accountRepository;
        }

        public async Task<(IdentityResult, RegisterResponseDto?)> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);

            if (registerDto.Password != registerDto.RepeatedPassword)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Passwords do not match" });
                return (result, null);
            }

            var existingUser = await _accountRepository.FindUserByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                var result = IdentityResult.Failed(new IdentityError { Code = "UsernameExists", Description = "Username already exists" });
                return (result, null);
            }

            existingUser = await _accountRepository.FindUserByEmailAsync(registerDto.Email);
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

            var createResult = await _accountRepository.CreateUserAsync(user, registerDto.Password);
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

            await _accountRepository.AddUserToRoleAsync(user, roleToAssign);
            _logger.LogInformation("Assigned role {Role} to user {Username}", roleToAssign, user.UserName);

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
                await _accountRepository.CreateStudentProfileAsync(student);
                _logger.LogInformation("Created student record for user: {Username}", user.UserName);
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
                await _accountRepository.CreateInstructorProfileAsync(instructor);
                _logger.LogInformation("Created instructor record for user: {Username}", user.UserName);
            }

            _logger.LogInformation("User registered successfully: {Username} with role {Role}", user.UserName, roleToAssign);
            var response = new RegisterResponseDto { Message = $"User registered successfully with {roleToAssign} role" };
            return (IdentityResult.Success, response);
        }

        public async Task<(TokenResponseDto?, string?)> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);

            var user = await _accountRepository.FindUserByUsernameAsync(loginDto.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed for username {Username}: Invalid username or password", loginDto.Username);
                return (null, "Invalid username or password");
            }

            var result = await _accountRepository.CheckPasswordAsync(user, loginDto.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed for username {Username}: Invalid username or password", loginDto.Username);
                return (null, "Invalid username or password");
            }

            var accessToken = await GenerateJwtToken(user);
            var (refreshTokenEntity, refreshToken) = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

            _logger.LogInformation("User {Username} logged in successfully", loginDto.Username);
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return (tokenResponse, null);
        }

        public async Task<(TokenResponseDto?, string?)> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest)
        {
            _logger.LogInformation("Token refresh attempt.");

            var (refreshTokenEntity, validationError) = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
            if (refreshTokenEntity == null)
            {
                _logger.LogWarning("Token refresh failed: {ValidationError}", validationError);
                return (null, validationError);
            }

            var user = await _accountRepository.FindUserByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: User not found for token.");
                return (null, "User not found");
            }

            var (newRefreshTokenEntity, newRefreshToken) = await _refreshTokenService.RotateRefreshTokenAsync(
                refreshTokenRequest.RefreshToken,
                user.Id);

            if (string.IsNullOrEmpty(newRefreshToken))
            {
                _logger.LogError("Token refresh failed for user {UserId}: Failed to rotate refresh token.", user.Id);
                return (null, "Failed to rotate refresh token");
            }

            var newAccessToken = await GenerateJwtToken(user);

            _logger.LogInformation("Token refreshed successfully for user {UserId}.", user.Id);
            var tokenResponse = new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
            return (tokenResponse, null);
        }

        public async Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
        {
            _logger.LogInformation("Token revocation attempt for user {UserId}.", userId);

            var tokenHash = _refreshTokenService.HashRefreshToken(revokeTokenRequest.RefreshToken);
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null)
            {
                _logger.LogWarning("Token revocation failed: Refresh token not found.");
                return (null, "Refresh token not found");
            }

            if (storedToken.UserId != userId)
            {
                _logger.LogWarning("Token revocation failed: Refresh token does not belong to the current user {UserId}.", userId);
                return (null, "Refresh token does not belong to the current user");
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Token revocation failed: Refresh token has already been revoked.");
                return (null, "Refresh token has already been revoked");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token revocation failed: Refresh token has expired.");
                return (null, "Refresh token has expired");
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            var response = new RevokeResponseDto { Message = "Refresh token revoked successfully" };
            return (response, null);
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            var roles = await _accountRepository.GetUserRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AppSettings:Token"] ?? string.Empty));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"],
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(TokenConstants.AccessTokenExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
