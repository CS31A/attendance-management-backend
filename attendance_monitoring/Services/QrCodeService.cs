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

    internal QrCodeService(
        QrCodeQueryService queryService,
        QrCodeWriteService writeService,
        QrCodeGenerationService generationService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _writeService = writeService ?? throw new ArgumentNullException(nameof(writeService));
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
    }

    #region Read Operations

    public Task<QrCodeResponseDto?> GetQrCodeByIdAsync(int id)
        => _queryService.GetQrCodeByIdAsync(id);

    public Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash)
        => _queryService.GetQrCodeByHashAsync(qrHash);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesByScheduleIdAsync(int scheduleId)
        => _queryService.GetQrCodesByScheduleIdAsync(scheduleId);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySectionIdAsync(int sectionId)
        => _queryService.GetQrCodesBySectionIdAsync(sectionId);

    public Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(int sessionId)
        => _queryService.GetQrCodesBySessionIdAsync(sessionId);

    public Task<IEnumerable<QrCodeResponseDto>> GetActiveQrCodesAsync()
        => _queryService.GetActiveQrCodesAsync();

    public Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(
        int qrCodeId, int instructorId, string userRole, int pageNumber = 1, int pageSize = 50)
        => _queryService.GetScanHistoryAsync(qrCodeId, instructorId, userRole, pageNumber, pageSize);

    public Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(
        string qrHash, int instructorId, string userRole, int pageNumber = 1, int pageSize = 50)
        => _queryService.GetScanHistoryByHashAsync(qrHash, instructorId, userRole, pageNumber, pageSize);

    #endregion

    #region Write Operations

    public Task<QrCodeResponseDto> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user)
        => _generationService.CreateQrCodeAsync(createQrCode, user);

    public Task<QrCodeGenerationResponseDto> GenerateQrCodeAsync(QrCodeRequest qrCodeRequest, ClaimsPrincipal user)
        => _generationService.GenerateQrCodeAsync(qrCodeRequest, user);

    public Task<QrCodeResponseDto> UpdateQrCodeAsync(int id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
        => _writeService.UpdateQrCodeAsync(id, updateQrCode, user);

    public Task DeactivateQrCodeAsync(int id, ClaimsPrincipal user)
        => _writeService.DeactivateQrCodeAsync(id, user);

    public Task RevokeQrCodeAsync(int id, string? reason, ClaimsPrincipal user)
        => _writeService.RevokeQrCodeAsync(id, reason, user);

    public Task DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
        => _writeService.DeactivateQrCodeByHashAsync(qrHash, user);

    public Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user)
        => _writeService.RevokeQrCodeByHashAsync(qrHash, reason, user);

    public Task ReactivateQrCodeAsync(int id, ClaimsPrincipal user)
        => _writeService.ReactivateQrCodeAsync(id, user);

    public Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
        => _writeService.ReactivateQrCodeByHashAsync(qrHash, user);

    public Task DeleteQrCodeAsync(int id, ClaimsPrincipal user)
        => _writeService.DeleteQrCodeAsync(id, user);

    #endregion

    #region Validation and Usage Operations

    public Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrHash)
        => _writeService.ValidateQrCodeAsync(qrHash);

    public Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user)
        => _generationService.ScanQrCodeAsync(validateQrCode, user);

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

    public Task<QrCodeResponseDto> ExtendQrCodeExpirationAsync(int id, int additionalMinutes, ClaimsPrincipal user)
        => _writeService.ExtendQrCodeExpirationAsync(id, additionalMinutes, user);

    #endregion
}
