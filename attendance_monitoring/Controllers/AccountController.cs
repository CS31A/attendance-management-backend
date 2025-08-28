using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.IServices;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext context,
        IRefreshTokenService refreshTokenService)
        : ControllerBase
    {
        // POST: api/account/register
        [HttpPost("register")]
        public async Task<ActionResult<object>> Register(RegisterDto registerDto)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that passwords match (additional check)
            if (registerDto.Password != registerDto.RepeatedPassword)
            {
                ModelState.AddModelError("RepeatedPassword", "Passwords do not match");
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await userManager.FindByNameAsync(registerDto.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username already exists");
                return BadRequest(ModelState);
            }

            existingUser = await userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return BadRequest(ModelState);
            }

            var user = new IdentityUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Create roles if they don't exist
            var validRoles = new[] { "Admin", "Teacher", "Student" };
            foreach (var role in validRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Assign role to user
            // Default to "Student" role as requested
            var roleToAssign = "Student";
            
            // If a specific role is provided, and it's valid, use that instead
            if (!string.IsNullOrEmpty(registerDto.Role) && validRoles.Contains(registerDto.Role, StringComparer.OrdinalIgnoreCase))
            {
                roleToAssign = registerDto.Role;
            }

            await userManager.AddToRoleAsync(user, roleToAssign);

            // If the user is a student, also create a student record
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

                context.Students.Add(student);
                await context.SaveChangesAsync();
            } else if (roleToAssign.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
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

                context.Instructors.Add(instructor);
                await context.SaveChangesAsync();
            }

            return Ok(new { Message = $"User registered successfully with {roleToAssign} role" });
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto loginDto)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
            {
                ModelState.AddModelError("Username", "Invalid username or password");
                return Unauthorized(ModelState);
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("Password", "Invalid username or password");
                return Unauthorized(ModelState);
            }

            var accessToken = await GenerateJwtToken(user);
            
            // Generate refresh token
            var (refreshTokenEntity, refreshToken) = await refreshTokenService.CreateRefreshTokenAsync(user.Id);

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // POST: api/account/refresh
        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto refreshTokenRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate the refresh token
            var (refreshTokenEntity, validationError) = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
            
            if (refreshTokenEntity == null)
            {
                return Unauthorized(new { Message = validationError });
            }

            // Get the user associated with the refresh token
            var user = await userManager.FindByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                return Unauthorized(new { Message = "User not found" });
            }

            // Rotate the refresh token
            var (newRefreshTokenEntity, newRefreshToken) = await refreshTokenService.RotateRefreshTokenAsync(
                refreshTokenRequest.RefreshToken, 
                user.Id);
            
            if (string.IsNullOrEmpty(newRefreshToken))
            {
                return Unauthorized(new { Message = "Failed to rotate refresh token" });
            }

            // Generate new access token
            var newAccessToken = await GenerateJwtToken(user);

            return Ok(new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        // POST: api/account/revoke
        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult> Revoke(RevokeTokenRequestDto revokeTokenRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the user ID from the claims
            var userId = GetUserId(User);
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not found" });
            }
            
            // Hash the refresh token to check what we're looking for
            var tokenHash = refreshTokenService.HashRefreshToken(revokeTokenRequest.RefreshToken);

            // Get the stored token to check its details
            var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
            if (storedToken == null)
            {
                return Unauthorized(new { Message = "Refresh token not found" });
            }

            // Check if token belongs to the current user
            if (storedToken.UserId != userId)
            {
                // Let's also check if we can find the user associated with the token
                var tokenUser = await userManager.FindByIdAsync(storedToken.UserId);
                if (tokenUser != null)
                {
                    return Unauthorized(new { Message = "Refresh token does not belong to the current user" });
                }
                else
                {
                    return Unauthorized(new { Message = "Refresh token does not belong to the current user" });
                }
            }

            // Check if token is already revoked
            if (storedToken.IsRevoked)
            {
                return Unauthorized(new { Message = "Refresh token has already been revoked" });
            }

            // Check if token has expired
            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { Message = "Refresh token has expired" });
            }

            // Revoke the refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            return Ok(new { Message = "Refresh token revoked successfully" });
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            // Add roles as claims
            var roles = await userManager.GetRolesAsync(user);
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
                expires: DateTime.UtcNow.AddMinutes(15), // Shorter expiration for access token
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        private string GetUserId(ClaimsPrincipal userPrincipal)
        {
            return userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}