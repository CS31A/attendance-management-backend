using attendance_monitoring.Classes;
using Dapper;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        /// <summary>
        /// This is experimental dapper implementation for possible migrations in the future.
        /// </summary>
        public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsyncBetter()
        {
            var sql = @"
                SELECT 
                    u.Id AS UserId,
                    u.UserName AS Username,
                    u.Email,
                    ISNULL(r.Name, 'Unknown') AS Role,
                    COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                    COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                    COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                    COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                    COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt
                FROM Users u
                LEFT JOIN UserRoles ur ON u.Id = ur.UserId
                LEFT JOIN Roles r ON ur.RoleId = r.Id
                LEFT JOIN Students s ON u.Id = s.UserId AND s.IsDeleted = 0
                LEFT JOIN Instructors i ON u.Id = i.UserId AND i.IsDeleted = 0
                LEFT JOIN Admins a ON u.Id = a.UserId
            ";

            var users = await context.Database.GetDbConnection().QueryAsync<GetAllUsersDto>(sql);
            return users;
        }

        public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync()
        {
            var users = await (
            from user in context.Users.AsNoTracking()
            
            // Get role
            join userRole in context.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in context.Roles on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            
            // Get student profile
            join student in context.Students.Where(s => !s.IsDeleted) 
                on user.Id equals student.UserId into students
            from student in students.DefaultIfEmpty()
            
            // Get instructor profile
            join instructor in context.Instructors.Where(i => !i.IsDeleted) 
                on user.Id equals instructor.UserId into instructors
            from instructor in instructors.DefaultIfEmpty()
            
            // Get admin profile
            join admin in context.Admins 
                on user.Id equals admin.UserId into admins
            from admin in admins.DefaultIfEmpty()
            
            select new GetAllUsersDto
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role.Name ?? "Unknown",
                ProfileId = student != null ? (int?)student.Id :
                            instructor != null ? (int?)instructor.Id :
                            admin != null ? (int?)admin.Id : null,
                Firstname = student != null ? student.Firstname :
                            instructor != null ? instructor.Firstname :
                            admin != null ? admin.Firstname : null,
                Lastname = student != null ? student.Lastname :
                        instructor != null ? instructor.Lastname :
                        admin != null ? admin.Lastname : null,
                CreatedAt = student != null ? student.CreatedAt :
                            instructor != null ? instructor.CreatedAt :
                            admin != null ? admin.CreatedAt : DateTime.UtcNow,
                UpdatedAt = student != null ? student.UpdatedAt :
                            instructor != null ? instructor.UpdatedAt :
                            admin != null ? admin.UpdatedAt : DateTime.UtcNow
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return users;

        }

        public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsyncSP()
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var users = await connection.QueryAsync<GetAllUsersDto>(
                "sp_GetAllUsers",
                commandType: CommandType.StoredProcedure
            );
            return users;
        }

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
                .AsNoTracking()
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
                .AsNoTracking()
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
