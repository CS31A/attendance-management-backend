using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        #region User Lookup Methods

        #region FindUserByIdAsync
        public async Task<IdentityUser?> FindUserByIdAsync(string id)
        {
            return await userManager.FindByIdAsync(id).ConfigureAwait(false);
        }
        #endregion

        #region FindUserByUsernameAsync
        public async Task<IdentityUser?> FindUserByUsernameAsync(string username)
        {
            return await userManager.FindByNameAsync(username).ConfigureAwait(false);
        }
        #endregion

        #region FindUserByEmailAsync
        public async Task<IdentityUser?> FindUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        }
        #endregion

        #endregion

        #region User Management Methods

        #region CreateUserAsync
        public async Task<IdentityResult> CreateUserAsync(IdentityUser user, string password)
        {
            return await userManager.CreateAsync(user, password).ConfigureAwait(false);
        }
        #endregion

        #region CheckPasswordAsync
        public async Task<SignInResult> CheckPasswordAsync(IdentityUser user, string password)
        {
            return await signInManager.CheckPasswordSignInAsync(user, password, false).ConfigureAwait(false);
        }
        #endregion

        #region EnsureRolesExistAsync
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
        #endregion

        #region AddUserToRoleAsync
        public async Task AddUserToRoleAsync(IdentityUser user, string role)
        {
            await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        }
        #endregion

        #region GetUserRolesAsync
        public async Task<IList<string>> GetUserRolesAsync(IdentityUser user)
        {
            return await userManager.GetRolesAsync(user).ConfigureAwait(false);
        }
        #endregion
        
        #region DeleteUserAsync
        public async Task<IdentityResult> DeleteUserAsync(IdentityUser user)
        {
            return await userManager.DeleteAsync(user).ConfigureAwait(false);
        }
        #endregion

        #endregion

        #region Profile Creation Methods

        #region CreateStudentProfileAsync
        public async Task CreateStudentProfileAsync(Student student)
        {
            context.Students.Add(student);
        }
        #endregion

        #region CreateInstructorProfileAsync
        public async Task CreateInstructorProfileAsync(Instructor instructor)
        {
            context.Instructors.Add(instructor);
        }
        #endregion

        #region CreateAdminProfileAsync
        public async Task CreateAdminProfileAsync(Admin admin)
        {
            context.Admins.Add(admin);
        }
        #endregion

        #endregion

        #region Token Management Methods

        #region FindRefreshTokenByHashAsync
        public async Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash)
        {
            return await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
        }
        #endregion

        #endregion

        #region Utility Methods

        #region SaveChangesAsync
        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync().ConfigureAwait(false);
        }
        #endregion

        #endregion
    }
}
