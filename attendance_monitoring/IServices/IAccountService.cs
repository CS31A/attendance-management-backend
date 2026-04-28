using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    /// <summary>
    /// Service interface for account management operations including authentication and user profiles.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Retrieves all users filtered by status.
        /// </summary>
        /// <param name="status">The user status to filter by.</param>
        /// <returns>A collection of user DTOs.</returns>
        Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active);

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="registerDto">The registration data.</param>
        /// <returns>The registration response.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails (password mismatch, missing fields, invalid section for role).</exception>
        /// <exception cref="EntityAlreadyExistsException{String}">Thrown when username or email already exists.</exception>
        /// <exception cref="EntityNotFoundException{Int32}">Thrown when the specified section does not exist.</exception>
        /// <exception cref="EntityServiceException">Thrown when user creation fails.</exception>
        Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// Authenticates a user and returns access tokens.
        /// </summary>
        /// <param name="loginDto">The login credentials.</param>
        /// <returns>The login result containing tokens and user info.</returns>
        /// <exception cref="ValidationException">Thrown when credentials are invalid.</exception>
        Task<LoginResult> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Refreshes access tokens using a refresh token.
        /// </summary>
        /// <param name="refreshTokenRequest">The refresh token request.</param>
        /// <returns>The new token response.</returns>
        /// <exception cref="ValidationException">Thrown when the refresh token is invalid.</exception>
        /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found.</exception>
        /// <exception cref="EntityServiceException">Thrown when token rotation fails.</exception>
        Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);

        /// <summary>
        /// Revokes a refresh token.
        /// </summary>
        /// <param name="revokeTokenRequest">The revoke token request.</param>
        /// <param name="userId">The user ID performing the revocation.</param>
        /// <returns>The revoke response.</returns>
        /// <exception cref="ValidationException">Thrown when the token is invalid or cannot be revoked.</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when the token doesn't belong to the user.</exception>
        /// <exception cref="EntityServiceException">Thrown when revocation fails due to database errors.</exception>
        Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);

        /// <summary>
        /// Logs out a user by blacklisting their access token.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="accessToken">The access token to blacklist.</param>
        /// <returns>The logout response.</returns>
        Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken);

        /// <summary>
        /// Logs out a web user by blacklisting their access token.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="accessToken">The access token to blacklist.</param>
        /// <returns>The logout response.</returns>
        Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken);

        /// <summary>
        /// Blacklists a JWT token.
        /// </summary>
        /// <param name="jti">The token's JTI claim.</param>
        /// <param name="expiresAt">The token's expiration time.</param>
        Task BlacklistTokenAsync(string jti, DateTime expiresAt);

        /// <summary>
        /// Retrieves a user's profile information.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user profile response.</returns>
        /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found.</exception>
        Task<UserProfileResponseDto> GetUserProfileAsync(string userId);

        /// <summary>
        /// Updates a user's own profile information
        /// </summary>
        /// <param name="userId">The ID of the user updating their profile</param>
        /// <param name="updateProfileDto">The profile update data</param>
        /// <returns>Updated user profile</returns>
        /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="EntityAlreadyExistsException{String}">Thrown when email is already in use</exception>
        Task<UserProfileResponseDto> UpdateUserProfileAsync(string userId, UpdateProfile updateProfileDto);

        /// <summary>
        /// Admin updates another user's profile information
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the update</param>
        /// <param name="adminUpdateDto">The profile update data including target user ID</param>
        /// <returns>Updated user profile</returns>
        /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        /// <exception cref="EntityAlreadyExistsException{String}">Thrown when email is already in use</exception>
        Task<UserProfileResponseDto> AdminUpdateUserProfileAsync(string adminId, AdminUpdateUser adminUpdateDto);

        /// <summary>
        /// Admin deletes a user (soft delete)
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the deletion</param>
        /// <param name="targetUserId">The ID of the user to delete</param>
        /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
        /// <exception cref="ValidationException">Thrown when trying to delete self</exception>
        /// <exception cref="EntityServiceException">Thrown when deletion fails</exception>
        Task AdminDeleteUserAsync(string adminId, string targetUserId);

        /// <summary>
        /// Admin permanently deletes a user and all associated data (hard delete)
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the deletion</param>
        /// <param name="targetUserId">The ID of the user to permanently delete</param>
        /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
        /// <exception cref="ValidationException">Thrown when trying to delete self</exception>
        /// <exception cref="EntityServiceException">Thrown when deletion fails</exception>
        Task AdminHardDeleteUserAsync(string adminId, string targetUserId);

        /// <summary>
        /// Admin restores a soft-deleted user (reactivates archived user)
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the restoration</param>
        /// <param name="targetUserId">The ID of the user to restore</param>
        /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
        /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
        /// <exception cref="ValidationException">Thrown when user is not deleted</exception>
        /// <exception cref="EntityServiceException">Thrown when restoration fails</exception>
        Task AdminRestoreUserAsync(string adminId, string targetUserId);
    }

    /// <summary>
    /// Result class for login operations.
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// The token response containing access and refresh tokens.
        /// </summary>
        public required TokenResponseDto TokenResponse { get; set; }

        /// <summary>
        /// The authenticated user's username.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// The authenticated user's role.
        /// </summary>
        public required string Role { get; set; }
    }
}
