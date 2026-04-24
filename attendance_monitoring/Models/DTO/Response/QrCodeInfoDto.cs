namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// QR code information included in scan history response
/// </summary>
public class QrCodeInfoDto
{
    /// <summary>
    /// QR Code ID
    /// </summary>
    public int Id { get; set; }
    public Guid? Uuid { get; set; }

    /// <summary>
    /// QR code hash (unique identifier)
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Associated session ID
    /// </summary>
    public int SessionId { get; set; }
    public Guid? SessionUuid { get; set; }

    /// <summary>
    /// Session date and time
    /// </summary>
    public DateTime SessionDate { get; set; }

    /// <summary>
    /// Session subject name
    /// </summary>
    public string SessionSubject { get; set; } = string.Empty;

    /// <summary>
    /// When the QR code was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When the QR code expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the QR code is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Total number of scans for this QR code
    /// </summary>
    public int TotalScans { get; set; }

    /// <summary>
    /// Number of unique students who scanned
    /// </summary>
    public int UniqueStudents { get; set; }
}
