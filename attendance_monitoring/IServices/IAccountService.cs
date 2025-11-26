using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.IServices
{
    public interface IAccountService
    {
        Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync();
        Task<(IdentityResult, RegisterResponseDto?)> RegisterAsync(RegisterDto registerDto);
        Task<(TokenResponseDto?, string?, string?, string?)> LoginAsync(LoginDto loginDto);
        Task<(TokenResponseDto?, string?)> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest);
        Task<(RevokeResponseDto?, string?)> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId);
        Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken);
        Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken);
        Task BlacklistTokenAsync(string jti, DateTime expiresAt);
        Task<(UserProfileResponseDto?, string?)> GetUserProfileAsync(string userId);

        /// <summary>
        /// Updates a user's own profile information
        /// </summary>
        /// <param name="userId">The ID of the user updating their profile</param>
        /// <param name="updateProfileDto">The profile update data</param>
        /// <returns>Tuple containing success status, updated profile, and error message if applicable</returns>
        Task<(bool Success, UserProfileResponseDto? Profile, string? ErrorMessage)> UpdateUserProfileAsync(string userId, UpdateProfile updateProfileDto);

        /// <summary>
        /// Admin updates another user's profile information
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the update</param>
        /// <param name="adminUpdateDto">The profile update data including target user ID</param>
        /// <returns>Tuple containing success status, updated profile, and error message if applicable</returns>
        Task<(bool Success, UserProfileResponseDto? Profile, string? ErrorMessage)> AdminUpdateUserProfileAsync(string adminId, AdminUpdateUser adminUpdateDto);

        /// <summary>
        /// Admin deletes a user (soft delete)
        /// </summary>
        /// <param name="adminId">The ID of the admin performing the deletion</param>
        /// <param name="targetUserId">The ID of the user to delete</param>
        /// <returns>Tuple containing success status and message</returns>
        Task<(bool Success, string Message)> AdminDeleteUserAsync(string adminId, string targetUserId);
    }
}
