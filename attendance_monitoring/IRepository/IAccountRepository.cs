using attendance_monitoring.Classes;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.IRepository
{
    /// <summary>
    /// Represents the repository for managing user accounts.
    /// </summary>
    public interface IAccountRepository : ISaveableRepository
    {
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

    }
}
