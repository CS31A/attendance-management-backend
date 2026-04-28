using System.Security.Claims;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.QrCode;

namespace attendance_monitoring.Services;

/// <summary>
/// Public facade for QR code operations used by controllers and other callers.
/// Delegates work to focused QR code units for query, write, and generation flows.
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly QrCodeQueryService _queryService;
    private readonly QrCodeWriteService _writeService;
    private readonly QrCodeGenerationService _generationService;
    private readonly QrCodeScanService _scanService;

    internal QrCodeService(
        QrCodeQueryService queryService,
        QrCodeWriteService writeService,
        QrCodeGenerationService generationService,
        QrCodeScanService scanService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _writeService = writeService ?? throw new ArgumentNullException(nameof(writeService));
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        _scanService = scanService ?? throw new ArgumentNullException(nameof(scanService));
    }

    #region Read Operations

    public Task<QrCodeResponseDto?> GetQrCodeByIdAsync(Guid id)
        => _queryService.GetQrCodeByIdAsync(id);

    public Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash)
        => _queryService.GetQrCodeByHashAsync(qrHash);

    public Task<QrCodeResponseDto?> GetQrCodeByUuidAsync(Guid id)
        => _queryService.GetQrCodeByUuidAsync(id);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesByScheduleIdAsync(Guid scheduleId)
        => _queryService.GetQrCodesByScheduleIdAsync(scheduleId);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySectionIdAsync(Guid sectionId)
        => _queryService.GetQrCodesBySectionIdAsync(sectionId);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(Guid sessionId)
        => _queryService.GetQrCodesBySessionIdAsync(sessionId);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionUuidAsync(Guid sessionUuid)
        => _queryService.GetQrCodesBySessionUuidAsync(sessionUuid);

    public Task<IEnumerable<QrCodeResponseDto>> GetActiveQrCodesAsync()
        => _queryService.GetActiveQrCodesAsync();

    public Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(
        Guid qrCodeId, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50)
        => _queryService.GetScanHistoryAsync(qrCodeId, instructorId, userRole, pageNumber, pageSize);

    public Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(
        string qrHash, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50)
        => _queryService.GetScanHistoryByHashAsync(qrHash, instructorId, userRole, pageNumber, pageSize);

    public Task<QrCodeScanHistoryResponseDto> GetScanHistoryByUuidAsync(
        Guid id, Guid instructorId, string userRole, int pageNumber = 1, int pageSize = 50)
        => _queryService.GetScanHistoryByUuidAsync(id, instructorId, userRole, pageNumber, pageSize);

    #endregion

    #region Write Operations

    public Task<QrCodeResponseDto> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user)
        => _generationService.CreateQrCodeAsync(createQrCode, user);

    public Task<QrCodeGenerationResponseDto> GenerateQrCodeAsync(QrCodeRequest qrCodeRequest, ClaimsPrincipal user)
        => _generationService.GenerateQrCodeAsync(qrCodeRequest, user);

    public Task<QrCodeResponseDto> UpdateQrCodeAsync(Guid id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
        => _writeService.UpdateQrCodeAsync(id, updateQrCode, user);

    public Task<QrCodeResponseDto> UpdateQrCodeByUuidAsync(Guid id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
        => _writeService.UpdateQrCodeByUuidAsync(id, updateQrCode, user);

    public Task DeactivateQrCodeAsync(Guid id, ClaimsPrincipal user)
        => _writeService.DeactivateQrCodeAsync(id, user);

    public Task RevokeQrCodeAsync(Guid id, string? reason, ClaimsPrincipal user)
        => _writeService.RevokeQrCodeAsync(id, reason, user);

    public Task RevokeQrCodeByUuidAsync(Guid id, string? reason, ClaimsPrincipal user)
        => _writeService.RevokeQrCodeByUuidAsync(id, reason, user);

    public Task DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
        => _writeService.DeactivateQrCodeByHashAsync(qrHash, user);

    public Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user)
        => _writeService.RevokeQrCodeByHashAsync(qrHash, reason, user);

    public Task ReactivateQrCodeAsync(Guid id, ClaimsPrincipal user)
        => _writeService.ReactivateQrCodeAsync(id, user);

    public Task ReactivateQrCodeByUuidAsync(Guid id, ClaimsPrincipal user)
        => _writeService.ReactivateQrCodeByUuidAsync(id, user);

    public Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
        => _writeService.ReactivateQrCodeByHashAsync(qrHash, user);

    public Task DeleteQrCodeAsync(Guid id, ClaimsPrincipal user)
        => _writeService.DeleteQrCodeAsync(id, user);

    public Task DeleteQrCodeByUuidAsync(Guid id, ClaimsPrincipal user)
        => _writeService.DeleteQrCodeByUuidAsync(id, user);

    public Task<QrCodeResponseDto> ExtendQrCodeExpirationByUuidAsync(Guid id, int additionalMinutes, ClaimsPrincipal user)
        => _writeService.ExtendQrCodeExpirationByUuidAsync(id, additionalMinutes, user);

    #endregion

    #region Validation and Usage Operations

    public Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrHash)
        => _writeService.ValidateQrCodeAsync(qrHash);

    public Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user)
        => _scanService.ScanQrCodeAsync(validateQrCode, user);

    public Task<bool> QrHashExistsAsync(string qrHash)
        => _generationService.QrHashExistsAsync(qrHash);

    #endregion

    #region Utility Operations

    public Task<string> GenerateUniqueQrHashAsync(string? clientHash = null)
        => _generationService.GenerateUniqueQrHashAsync(clientHash);

    public Task<int> CleanupExpiredQrCodesAsync(ClaimsPrincipal user)
        => _writeService.CleanupExpiredQrCodesAsync(user);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesExpiringSoonAsync(int expiringWithinMinutes = 30)
        => _writeService.GetQrCodesExpiringSoonAsync(expiringWithinMinutes);

    public Task<QrCodeResponseDto> ExtendQrCodeExpirationAsync(Guid id, int additionalMinutes, ClaimsPrincipal user)
        => _writeService.ExtendQrCodeExpirationAsync(id, additionalMinutes, user);

    #endregion
}
