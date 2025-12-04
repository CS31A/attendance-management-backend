using System.Threading.Tasks;

namespace attendance_monitoring.IServices
{
    /// <summary>
    /// Represents the result of a user creation operation.
    /// </summary>
    public class UserCreationResult
    {
        /// <summary>
        /// Gets or sets whether the user creation operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the ID of the created user if the operation was successful.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets any error messages if the operation was unsuccessful.
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Factory for creating user instances with associated roles (Student, Instructor, Admin)
    /// in the attendance management system.
    /// </summary>
    public interface IUserFactory
    {
        /// <summary>
        /// Creates a new user with the specified role and profile in the attendance management system.
        /// </summary>
        /// <param name="username">The username for the new user</param>
        /// <param name="email">The email address for the new user</param>
        /// <param name="password">The password for the new user</param>
        /// <param name="role">The role to assign to the user (Student, Instructor, or Admin)</param>
        /// <param name="firstName">The first name of the user (optional)</param>
        /// <param name="lastName">The last name of the user (optional)</param>
        /// <param name="sectionId">The section ID for students (required when role is Student)</param>
        /// <returns>A UserCreationResult indicating success or failure with any error messages</returns>
        Task<UserCreationResult> CreateUserAsync(string username, string email, string password, string role, string? firstName = null, string? lastName = null, int? sectionId = null);
    }
}