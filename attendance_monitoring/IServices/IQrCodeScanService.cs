using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;

namespace attendance_monitoring.IServices;

public interface IQrCodeScanService
{
    Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user);
}
