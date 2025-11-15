namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Complete response for QR code scan history with pagination
/// </summary>
public class QrCodeScanHistoryResponseDto
{
    /// <summary>
    /// QR code information
    /// </summary>
    public QrCodeInfoDto QrCodeInfo { get; set; } = new();

    /// <summary>
    /// Scan statistics and aggregates
    /// </summary>
    public QrCodeStatisticsDto ScanStatistics { get; set; } = new();

    /// <summary>
    /// Paginated list of scan history items
    /// </summary>
    public PagedResult<QrCodeScanHistoryItemDto> Scans { get; set; } = new();
}
