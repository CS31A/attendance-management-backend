using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Repositories
{
    public class AccountRepository(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        ILogger<AccountRepository> logger)
        : IAccountRepository
    {
        public async Task<IdentityUser?> FindUserByIdAsync(string id)
        {
            return await userManager.FindByIdAsync(id).ConfigureAwait(false);
        }

        public async Task<IdentityUser?> FindUserByUsernameAsync(string username)
        {
            return await userManager.FindByNameAsync(username).ConfigureAwait(false);
        }

        public async Task<IdentityUser?> FindUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        }

        public async Task<IdentityResult> CreateUserAsync(IdentityUser user, string password)
        {
            return await userManager.CreateAsync(user, password).ConfigureAwait(false);
        }

        public async Task<SignInResult> CheckPasswordAsync(IdentityUser user, string password)
        {
            return await signInManager.CheckPasswordSignInAsync(user, password, false).ConfigureAwait(false);
        }

        public async Task EnsureRolesExistAsync(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                {
                    await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
                }
            }
        }

        public async Task AddUserToRoleAsync(IdentityUser user, string role)
        {
            await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        }

        public async Task<IList<string>> GetUserRolesAsync(IdentityUser user)
        {
            return await userManager.GetRolesAsync(user).ConfigureAwait(false);
        }

        public async Task CreateStudentProfileAsync(Student student)
        {
            context.Students.Add(student);
            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Rare for inserts; log as warning
                logger.LogWarning(ex, "Concurrency issue while creating student profile for user {UserId}", student.UserId);
            }
            catch (DbUpdateException ex)
            {
                // Likely constraint violation, log as error
                logger.LogError(ex, "Database update failed while creating student profile for user {UserId}", student.UserId);
            }
        }

        public async Task CreateInstructorProfileAsync(Instructor instructor)
        {
            context.Instructors.Add(instructor);
            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency issue while creating instructor profile for user {UserId}", instructor.UserId);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database update failed while creating instructor profile for user {UserId}", instructor.UserId);
            }
        }

        public async Task CreateAdminProfileAsync(Admin admin)
        {
            context.Admins.Add(admin);
            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency issue while creating admin profile for user {UserId}", admin.UserId);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database update failed while creating admin profile for user {UserId}", admin.UserId);
            }
        }
        
        public async Task<IdentityResult> DeleteUserAsync(IdentityUser user)
        {
            return await userManager.DeleteAsync(user).ConfigureAwait(false);
        }
        
        public async Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash)
        {
            return await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
        }
    }
}
