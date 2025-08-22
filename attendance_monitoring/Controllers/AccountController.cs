using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO;

namespace attendance_monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext context)
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
            }

            return Ok(new { Message = $"User registered successfully with {roleToAssign} role" });
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(LoginDto loginDto)
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

            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
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
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}