using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;

namespace attendance_monitoring.IServices;

public interface IQrCodeGenerationService
{
    Task<QrCodeGenerationResponseDto> GenerateQrCodeAsync(QrCodeRequest qrCodeRequest, ClaimsPrincipal user);
}
