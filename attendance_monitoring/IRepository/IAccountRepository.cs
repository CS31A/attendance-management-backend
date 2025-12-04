using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.IRepository
{
    /// <summary>
    /// Represents the repository for managing user accounts.
    /// </summary>
    public interface IAccountRepository : ISaveableRepository
    {

        /// <summary>
        /// Retrieves all users with role-specific information using projection.
        /// </summary>
        /// <returns>A collection of user DTOs with role and profile information.</returns>
        Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync();

        /// <summary>
        /// Retrieves all users with role-specific information using stored procedure.
        /// </summary>
        /// <returns>A collection of user DTOs with role and profile information.</returns>
        Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsyncSP();

        /// <summary>
        /// Finds a user by their ID.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<IdentityUser?> FindUserByIdAsync(string id);

        /// <summary>
        /// Finds a user by their username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<IdentityUser?> FindUserByUsernameAsync(string username);

        /// <summary>
        /// Finds a user by their email address.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<IdentityUser?> FindUserByEmailAsync(string email);

        /// <summary>
        /// Creates a new user with the given password.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>The result of the user creation operation.</returns>
        Task<IdentityResult> CreateUserAsync(IdentityUser user, string password);

        /// <summary>
        /// Checks the user's password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password to check.</param>
        /// <returns>The result of the password check.</returns>
        Task<SignInResult> CheckPasswordAsync(IdentityUser user, string password);

        /// <summary>
        /// Ensures that the specified roles exist.
        /// </summary>
        /// <param name="roles">The roles to ensure exist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureRolesExistAsync(IEnumerable<string> roles);

        /// <summary>
        /// Adds a user to the specified role.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="role">The role to add the user to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddUserToRoleAsync(IdentityUser user, string role);

        /// <summary>
        /// Gets the roles for the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A list of roles for the user.</returns>
        Task<IList<string>> GetUserRolesAsync(IdentityUser user);

        /// <summary>
        /// Creates a student profile.
        /// </summary>
        /// <param name="student">The student profile to create.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateStudentProfileAsync(Student student);

        /// <summary>
        /// Creates an instructor profile.
        /// </summary>
        /// <param name="instructor">The instructor profile to create.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateInstructorProfileAsync(Instructor instructor);

        Task CreateAdminProfileAsync(Admin admin);
        
        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <returns>The result of the user deletion operation.</returns>
        Task<IdentityResult> DeleteUserAsync(IdentityUser user);
        
        /// <summary>
        /// Finds a refresh token by its hash value.
        /// </summary>
        /// <param name="tokenHash">The hashed refresh token.</param>
        /// <returns>The refresh token entity if found; otherwise, null.</returns>
        Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash);

        /// <summary>
        /// Checks if an email address already exists, optionally excluding a specific user.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <param name="excludeUserId">Optional user ID to exclude from the check (for updates).</param>
        /// <returns>True if the email exists; otherwise, false.</returns>
        Task<bool> EmailExistsAsync(string email, string? excludeUserId = null);

        /// <summary>
        /// Updates a user's information.
        /// </summary>
        /// <param name="user">The user with updated information.</param>
        /// <returns>The result of the update operation.</returns>
        Task<IdentityResult> UpdateUserAsync(IdentityUser user);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="user">The user whose password to change.</param>
        /// <param name="currentPassword">The user's current password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>The result of the password change operation.</returns>
        Task<IdentityResult> ChangePasswordAsync(IdentityUser user, string currentPassword, string newPassword);

        /// <summary>
        /// Gets a student profile by user ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The student profile if found; otherwise, null.</returns>
        Task<Student?> GetStudentByUserIdAsync(string userId);

        /// <summary>
        /// Gets an instructor profile by user ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The instructor profile if found; otherwise, null.</returns>
        Task<Instructor?> GetInstructorByUserIdAsync(string userId);

        /// <summary>
        /// Updates a student profile.
        /// </summary>
        /// <param name="student">The student profile with updated information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateStudentProfileAsync(Student student);

        /// <summary>
        /// Updates an instructor profile.
        /// </summary>
        /// <param name="instructor">The instructor profile with updated information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateInstructorProfileAsync(Instructor instructor);

        /// <summary>
        /// Resets a user's password (admin operation - no current password required).
        /// </summary>
        /// <param name="user">The user whose password to reset.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>The result of the password reset operation.</returns>
        Task<IdentityResult> AdminResetPasswordAsync(IdentityUser user, string newPassword);

        /// <summary>
        /// Soft deletes a user by marking their profile as deleted using stored procedure.
        /// </summary>
        /// <param name="userId">The user ID to delete.</param>
        /// <returns>Tuple containing success flag and message.</returns>
        Task<(bool Success, string Message)> DeleteUserAsyncSP(string userId);

        /// <summary>
        /// Hard deletes a user and all associated data permanently using stored procedure.
        /// </summary>
        /// <param name="userId">The user ID to permanently delete.</param>
        /// <returns>Tuple containing success flag and message.</returns>
        Task<(bool Success, string Message)> HardDeleteUserAsyncSP(string userId);

        /// <summary>
        /// Updates user profile using stored procedure.
        /// </summary>
        /// <param name="userId">The user ID to update.</param>
        /// <param name="email">New email (optional).</param>
        /// <param name="firstname">New firstname (optional).</param>
        /// <param name="lastname">New lastname (optional).</param>
        /// <param name="sectionId">New section ID for students (optional).</param>
        /// <param name="isRegular">New isRegular status for students (optional).</param>
        /// <returns>Tuple containing success flag, updated user DTO, and message.</returns>
        Task<(bool Success, GetAllUsersDto? User, string Message)> UpdateUserAsyncSP(
            string userId,
            string? email = null,
            string? firstname = null,
            string? lastname = null,
            int? sectionId = null,
            bool? isRegular = null);
    }
}
