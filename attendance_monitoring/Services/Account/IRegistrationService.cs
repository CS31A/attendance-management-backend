using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

internal interface IRegistrationService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto);
}
