namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents a single QR code scan entry in the scan history
/// </summary>
public class QrCodeScanHistoryItemDto
{
    /// <summary>
    /// Attendance record ID (primary key)
    /// </summary>
    public int AttendanceRecordId { get; set; }

    /// <summary>
    /// Student ID who scanned the QR code
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// Student ID number (e.g., "2021-001234")
    /// </summary>
    public string StudentIdNumber { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the student
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the QR code was scanned
    /// </summary>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// Attendance status (Present, Late, Excused, Absent)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Session ID associated with this scan
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Session date and time
    /// </summary>
    public DateTime SessionDate { get; set; }

    /// <summary>
    /// Session subject name
    /// </summary>
    public string SessionSubject { get; set; } = string.Empty;

    /// <summary>
    /// Session section name
    /// </summary>
    public string SessionSection { get; set; } = string.Empty;
}
