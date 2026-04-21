using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

internal interface IAdminService
{
    Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active);
    Task<UserProfileResponseDto> AdminUpdateUserProfileAsync(string adminId, AdminUpdateUser adminUpdateDto);
    Task AdminDeleteUserAsync(string adminId, string targetUserId);
    Task AdminHardDeleteUserAsync(string adminId, string targetUserId);
    Task AdminRestoreUserAsync(string adminId, string targetUserId);
}
