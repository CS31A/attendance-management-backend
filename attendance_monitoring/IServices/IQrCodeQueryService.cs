using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface IQrCodeQueryService
{
    Task<QrCodeResponseDto?> GetQrCodeByIdAsync(Guid id);
    Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash);
    Task<QrCodeResponseDto?> GetQrCodeByUuidAsync(Guid id);
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(Guid sessionId);
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionUuidAsync(Guid sessionUuid);
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(Guid qrCodeId, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50);
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryByUuidAsync(Guid id, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50);
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(string qrHash, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50);
}
