using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using Dapper;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

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
                    COALESCE(s.Uuid, i.Uuid, a.Uuid) AS ProfileUuid,
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

        public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active)
        {
            var users = await (
            from user in context.Users.AsNoTracking()

                // Get role
            join userRole in context.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in context.Roles on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()

                // Get student profile - filter based on status
            join student in context.Students
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Course)
                .Where(s =>
                status == UserStatus.All ||
                (status == UserStatus.Active && !s.IsDeleted) ||
                (status == UserStatus.Archived && s.IsDeleted))
                on user.Id equals student.UserId into students
            from student in students.DefaultIfEmpty()

                // Get instructor profile - filter based on status
            join instructor in context.Instructors.Where(i =>
                status == UserStatus.All ||
                (status == UserStatus.Active && !i.IsDeleted) ||
                (status == UserStatus.Archived && i.IsDeleted))
                on user.Id equals instructor.UserId into instructors
            from instructor in instructors.DefaultIfEmpty()

                // Get admin profile - filter based on status
            join admin in context.Admins.Where(a =>
                status == UserStatus.All ||
                (status == UserStatus.Active && !a.IsDeleted) ||
                (status == UserStatus.Archived && a.IsDeleted))
                on user.Id equals admin.UserId into admins
            from admin in admins.DefaultIfEmpty()

            select new GetAllUsersDto
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = string.IsNullOrEmpty(role.Name) ? "Unknown" : RoleConstants.NormalizeRole(role.Name),
                StudentProfile = student != null ? new StudentProfileDto
                {
                    Id = student.Id,
                    Uuid = student.Uuid,
                    Firstname = student.Firstname,
                    Lastname = student.Lastname,
                    IsRegular = student.IsRegular,
                    SectionId = student.SectionId,
                    SectionName = student.Section != null ? student.Section.Name : null,
                    CourseId = student.Section != null && student.Section.Course != null ? student.Section.Course.Id : null,
                    CourseName = student.Section != null && student.Section.Course != null ? student.Section.Course.Name : null,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt,
                    IsDeleted = student.IsDeleted,
                    DeletedAt = student.DeletedAt
                } : null,
                InstructorProfile = instructor != null ? new InstructorProfileDto
                {
                    Id = instructor.Id,
                    Uuid = instructor.Uuid,
                    Firstname = instructor.Firstname,
                    Lastname = instructor.Lastname,
                    Department = instructor.Department,
                    CreatedAt = instructor.CreatedAt,
                    UpdatedAt = instructor.UpdatedAt,
                    IsDeleted = instructor.IsDeleted,
                    DeletedAt = instructor.DeletedAt
                } : null,
                AdminProfile = admin != null ? new AdminProfileDto
                {
                    Id = admin.Id,
                    Uuid = admin.Uuid,
                    Firstname = admin.Firstname,
                    Lastname = admin.Lastname,
                    CreatedAt = admin.CreatedAt,
                    UpdatedAt = admin.UpdatedAt,
                    IsDeleted = admin.IsDeleted,
                    DeletedAt = admin.DeletedAt
                } : null
            })
            .Where(u =>
                // Only include users that have a profile matching the status filter
                // Users without profiles are orphaned records and should be excluded unless status is All
                status == UserStatus.All || u.StudentProfile != null || u.InstructorProfile != null || u.AdminProfile != null)
            .ToListAsync()
            .ConfigureAwait(false);

            return users;

        }

        public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsyncSP(UserStatus status = UserStatus.Active)
        {
            var parameters = new { Status = (int)status };

            // The stored procedure returns flat data, so we need to map it to the new structure
            var flatResults = await QueryStoredProcedureAsync<UserSpResultDto>(
                "sp_GetAllUsers",
                parameters).ConfigureAwait(false);

            // Map flat results to the new profile-based structure
            var users = flatResults.Select(r => new GetAllUsersDto
            {
                UserId = r.UserId,
                Username = r.Username,
                Email = r.Email,
                Role = string.IsNullOrEmpty(r.Role) ? "Unknown" : RoleConstants.NormalizeRole(r.Role),
                StudentProfile = r.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) ? new StudentProfileDto
                {
                    Id = r.ProfileId ?? 0,
                    Uuid = r.ProfileUuid ?? Guid.Empty,
                    Firstname = r.Firstname ?? string.Empty,
                    Lastname = r.Lastname ?? string.Empty,
                    SectionId = r.SectionId ?? 0,
                    SectionName = r.SectionName,
                    CourseId = r.CourseId,
                    CourseName = r.CourseName,
                    IsRegular = r.IsRegular ?? false,
                    CreatedAt = r.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = r.UpdatedAt ?? DateTime.UtcNow,
                    IsDeleted = r.IsDeleted ?? false,
                    DeletedAt = r.DeletedAt
                } : null,
                InstructorProfile = RoleConstants.IsInstructorRole(r.Role) ? new InstructorProfileDto
                {
                    Id = r.ProfileId ?? 0,
                    Uuid = r.ProfileUuid ?? Guid.Empty,
                    Firstname = r.Firstname,
                    Lastname = r.Lastname,
                    Department = r.Department,
                    CreatedAt = r.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = r.UpdatedAt ?? DateTime.UtcNow,
                    IsDeleted = r.IsDeleted ?? false,
                    DeletedAt = r.DeletedAt
                } : null,
                AdminProfile = r.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? new AdminProfileDto
                {
                    Id = r.ProfileId ?? 0,
                    Uuid = r.ProfileUuid ?? Guid.Empty,
                    Firstname = r.Firstname,
                    Lastname = r.Lastname,
                    CreatedAt = r.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = r.UpdatedAt ?? DateTime.UtcNow,
                    IsDeleted = r.IsDeleted ?? false,
                    DeletedAt = r.DeletedAt
                } : null
            }).ToList();

            return users;
        }

        /// <summary>
        /// Internal DTO for mapping flat stored procedure results
        /// </summary>
        private class UserSpResultDto
        {
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int? ProfileId { get; set; }
            public Guid? ProfileUuid { get; set; }
            public string? Firstname { get; set; }
            public string? Lastname { get; set; }
            public string? Department { get; set; }
            public int? SectionId { get; set; }
            public string? SectionName { get; set; }
            public int? CourseId { get; set; }
            public string? CourseName { get; set; }
            public bool? IsRegular { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool? IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
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
            foreach (var role in roles.Select(RoleConstants.NormalizeRole).Distinct(StringComparer.OrdinalIgnoreCase))
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
            await userManager.AddToRoleAsync(user, RoleConstants.NormalizeRole(role)).ConfigureAwait(false);
        }
        #endregion

        #region GetUserRolesAsync
        public async Task<IList<string>> GetUserRolesAsync(IdentityUser user)
        {
            return (await userManager.GetRolesAsync(user).ConfigureAwait(false))
                .Select(RoleConstants.NormalizeRole)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
        /// <summary>
        /// Gets a student by user ID without eager loading navigation properties.
        /// Performance optimization: Section and Course are not loaded since no callers depend on these navigation properties.
        /// All callers only use student.Id, student.IsDeleted, or basic properties like Firstname, Lastname, SectionId (FK).
        /// </summary>
        public async Task<Student?> GetStudentByUserIdAsync(string userId)
        {
            return await context.Students
                .AsNoTracking()
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

        #region GetAdminByUserIdAsync
        public async Task<Admin?> GetAdminByUserIdAsync(string userId)
        {
            return await context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId && !a.IsDeleted)
                .ConfigureAwait(false);
        }
        #endregion

        #region GetAdminByUuidAsync
        public async Task<Admin?> GetAdminByUuidAsync(Guid uuid)
        {
            return await context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Uuid == uuid && !a.IsDeleted)
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

        #region UpdateAdminProfileAsync
        public Task UpdateAdminProfileAsync(Admin admin)
        {
            admin.UpdatedAt = DateTime.UtcNow;
            context.Admins.Update(admin);
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

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Soft deletes a user by marking their profile as deleted using stored procedure
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteUserAsyncSP(string userId)
        {
            var parameters = new { UserId = userId };
            var result = await QueryFirstOrDefaultStoredProcedureAsync<dynamic>(
                "sp_DeleteUser",
                parameters).ConfigureAwait(false);

            if (result != null)
            {
                bool success = result.Success;
                string message = result.Message ?? "Unknown error";
                return (success, message);
            }

            return (false, "No response from stored procedure");
        }

        /// <summary>
        /// Hard deletes a user and all associated data permanently using stored procedure
        /// </summary>
        public async Task<(bool Success, string Message)> HardDeleteUserAsyncSP(string userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.String, ParameterDirection.Input);
            parameters.Add("@ConfirmDeletion", true, DbType.Boolean, ParameterDirection.Input);
            parameters.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            await ExecuteStoredProcedureAsync("sp_HardDeleteUser", parameters).ConfigureAwait(false);

            bool success = parameters.Get<bool>("@Success");
            string message = parameters.Get<string>("@Message") ?? "Unknown error";

            return (success, message);
        }

        /// <summary>
        /// Restores a soft-deleted user using stored procedure
        /// </summary>
        /// <param name="userId">The user ID to restore.</param>
        /// <returns>Tuple containing success flag and message.</returns>
        public async Task<(bool Success, string Message)> RestoreUserAsyncSP(string userId)
        {
            var parameters = new { UserId = userId };
            var result = await QueryFirstOrDefaultStoredProcedureAsync<dynamic>(
                "sp_RestoreUser",
                parameters).ConfigureAwait(false);

            if (result != null)
            {
                bool success = result.Success;
                string message = result.Message ?? "Unknown error";
                return (success, message);
            }

            return (false, "No response from stored procedure");
        }

        /// <summary>
        /// Updates user profile using stored procedure
        /// </summary>
        public async Task<(bool Success, GetAllUsersDto? User, string Message)> UpdateUserAsyncSP(
            string userId,
            string? email = null,
            string? firstname = null,
            string? lastname = null,
            int? sectionId = null,
            bool? isRegular = null)
        {
            try
            {
                var parameters = new
                {
                    UserId = userId,
                    Email = email,
                    Firstname = firstname,
                    Lastname = lastname,
                    SectionId = sectionId,
                    IsRegular = isRegular
                };
                var result = await QueryFirstOrDefaultStoredProcedureAsync<dynamic>(
                    "sp_UpdateUser",
                    parameters).ConfigureAwait(false);

                if (result != null)
                {
                    bool success = result.Success;
                    string message = result.Message ?? "Unknown error";

                    if (success)
                    {
                        var userDto = new GetAllUsersDto
                        {
                            UserId = result.UserId,
                            Username = result.Username,
                            Email = result.Email,
                            Role = result.Role
                        };

                        // Populate appropriate profile based on role
                        string role = result.Role?.ToString() ?? "";
                        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                        {
                            userDto.StudentProfile = new StudentProfileDto
                            {
                                Id = result.ProfileId,
                                Uuid = result.ProfileUuid ?? Guid.Empty,
                                Firstname = result.Firstname ?? string.Empty,
                                Lastname = result.Lastname ?? string.Empty,
                                CreatedAt = result.CreatedAt,
                                UpdatedAt = result.UpdatedAt
                            };
                        }
                        else if (RoleConstants.IsInstructorRole(role))
                        {
                            userDto.InstructorProfile = new InstructorProfileDto
                            {
                                Id = result.ProfileId,
                                Uuid = result.ProfileUuid ?? Guid.Empty,
                                Firstname = result.Firstname,
                                Lastname = result.Lastname,
                                CreatedAt = result.CreatedAt,
                                UpdatedAt = result.UpdatedAt
                            };
                        }
                        else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            userDto.AdminProfile = new AdminProfileDto
                            {
                                Id = result.ProfileId,
                                Uuid = result.ProfileUuid ?? Guid.Empty,
                                Firstname = result.Firstname,
                                Lastname = result.Lastname,
                                CreatedAt = result.CreatedAt,
                                UpdatedAt = result.UpdatedAt
                            };
                        }

                        return (true, userDto, message);
                    }

                    return (false, null, message);
                }

                return (false, null, "No response from stored procedure");
            }
            catch (Exception ex)
            {
                return (false, null, $"Database error: {ex.Message}");
            }
        }
        #endregion

        private async Task<IEnumerable<T>> QueryStoredProcedureAsync<T>(string storedProcedure, object? parameters = null)
        {
            return await ExecuteWithManagedConnectionAsync((connection, transaction) =>
                connection.QueryAsync<T>(
                    storedProcedure,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        private async Task<T?> QueryFirstOrDefaultStoredProcedureAsync<T>(string storedProcedure, object? parameters = null)
        {
            return await ExecuteWithManagedConnectionAsync((connection, transaction) =>
                connection.QueryFirstOrDefaultAsync<T>(
                    storedProcedure,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        private async Task<int> ExecuteStoredProcedureAsync(string storedProcedure, object? parameters = null)
        {
            return await ExecuteWithManagedConnectionAsync((connection, transaction) =>
                connection.ExecuteAsync(
                    storedProcedure,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        private async Task<T> ExecuteWithManagedConnectionAsync<T>(Func<DbConnection, DbTransaction?, Task<T>> operation)
        {
            var connection = context.Database.GetDbConnection();
            var shouldCloseConnection = await OpenConnectionIfNeededAsync().ConfigureAwait(false);

            try
            {
                return await operation(connection, GetCurrentDbTransaction()).ConfigureAwait(false);
            }
            finally
            {
                await CloseConnectionIfOwnedAsync(shouldCloseConnection).ConfigureAwait(false);
            }
        }

        private async Task<bool> OpenConnectionIfNeededAsync()
        {
            var connection = context.Database.GetDbConnection();
            var hasActiveTransaction = context.Database.CurrentTransaction != null;

            if (connection.State == ConnectionState.Open)
            {
                return false;
            }

            await context.Database.OpenConnectionAsync().ConfigureAwait(false);
            return !hasActiveTransaction;
        }

        private async Task CloseConnectionIfOwnedAsync(bool shouldCloseConnection)
        {
            if (!shouldCloseConnection)
            {
                return;
            }

            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        private DbTransaction? GetCurrentDbTransaction()
        {
            return context.Database.CurrentTransaction?.GetDbTransaction();
        }

    }
}
