using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;

namespace attendance_monitoring.IServices;

public interface IQrCodeWriteService
{
    Task RevokeQrCodeAsync(Guid id, string? reason, ClaimsPrincipal user);
    Task RevokeQrCodeByUuidAsync(Guid id, string? reason, ClaimsPrincipal user);
    Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user);
    Task ReactivateQrCodeAsync(Guid id, ClaimsPrincipal user);
    Task ReactivateQrCodeByUuidAsync(Guid id, ClaimsPrincipal user);
    Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user);
    Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrHash);
}
