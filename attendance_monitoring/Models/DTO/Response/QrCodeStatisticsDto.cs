namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Statistical information about QR code scans
/// </summary>
public class QrCodeStatisticsDto
{
    /// <summary>
    /// Total number of scans (including duplicates if allowed)
    /// </summary>
    public int TotalScans { get; set; }

    /// <summary>
    /// Number of unique students who scanned
    /// </summary>
    public int UniqueStudents { get; set; }

    /// <summary>
    /// Number of scans with "Present" status
    /// </summary>
    public int PresentCount { get; set; }

    /// <summary>
    /// Number of scans with "Late" status
    /// </summary>
    public int LateCount { get; set; }

    /// <summary>
    /// Number of scans with "Excused" status
    /// </summary>
    public int ExcusedCount { get; set; }

    /// <summary>
    /// Timestamp of the first scan
    /// </summary>
    public DateTime? FirstScanAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent scan
    /// </summary>
    public DateTime? LastScanAt { get; set; }

    /// <summary>
    /// Average scan time (calculated from all scans)
    /// </summary>
    public DateTime? AverageScanTime { get; set; }
}
