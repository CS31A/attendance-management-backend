using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

internal interface IProfileService
{
    Task<UserProfileResponseDto> GetUserProfileAsync(string userId);
    Task<UserProfileResponseDto> UpdateUserProfileAsync(string userId, UpdateProfile updateProfileDto);
}
