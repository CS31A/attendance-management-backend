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
        ApplicationDbContext context
        )
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
        public Task CreateStudentProfileAsync(Student student)
        {
            context.Students.Add(student);
            return Task.CompletedTask;
        }
        #endregion

        #region CreateInstructorProfileAsync
        public Task CreateInstructorProfileAsync(Instructor instructor)
        {
            context.Instructors.Add(instructor);
            return Task.CompletedTask;
        }
        #endregion

        #region CreateAdminProfileAsync
        public Task CreateAdminProfileAsync(Admin admin)
        {
            context.Admins.Add(admin);
            return Task.CompletedTask;
        }
        #endregion

        #endregion

        #region Token Management Methods

        #region FindRefreshTokenByHashAsync
        public async Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash)
        {
            return await context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
        }
        #endregion

        #endregion

        #region User Update Methods

        #region EmailExistsAsync
        public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
        {
            var existingUser = await userManager.FindByEmailAsync(email).ConfigureAwait(false);

            if (existingUser == null)
            {
                return false;
            }

            // If excludeUserId is provided, check if the found user is the excluded one
            if (!string.IsNullOrEmpty(excludeUserId) && existingUser.Id == excludeUserId)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region UpdateUserAsync
        public async Task<IdentityResult> UpdateUserAsync(IdentityUser user)
        {
            return await userManager.UpdateAsync(user).ConfigureAwait(false);
        }
        #endregion

        #region ChangePasswordAsync
        public async Task<IdentityResult> ChangePasswordAsync(IdentityUser user, string currentPassword, string newPassword)
        {
            return await userManager.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);
        }
        #endregion

        #region GetStudentByUserIdAsync
        public async Task<Student?> GetStudentByUserIdAsync(string userId)
        {
            return await context.Students
                .Include(s => s.Section)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted)
                .ConfigureAwait(false);
        }
        #endregion

        #region GetInstructorByUserIdAsync
        public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
        {
            return await context.Instructors
                .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted)
                .ConfigureAwait(false);
        }
        #endregion

        #region UpdateStudentProfileAsync
        public Task UpdateStudentProfileAsync(Student student)
        {
            student.UpdatedAt = DateTime.UtcNow;
            context.Students.Update(student);
            return Task.CompletedTask;
        }
        #endregion

        #region UpdateInstructorProfileAsync
        public Task UpdateInstructorProfileAsync(Instructor instructor)
        {
            instructor.UpdatedAt = DateTime.UtcNow;
            context.Instructors.Update(instructor);
            return Task.CompletedTask;
        }
        #endregion

        #region AdminResetPasswordAsync
        public async Task<IdentityResult> AdminResetPasswordAsync(IdentityUser user, string newPassword)
        {
            // Generate password reset token
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);

            // Reset password using the token
            return await userManager.ResetPasswordAsync(user, resetToken, newPassword).ConfigureAwait(false);
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
