using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for scanning a fingerprint for attendance.
/// </summary>
public class ScanFingerprint
{
    /// <summary>
    /// The fingerprint template data captured from the biometric scan.
    /// This will be matched against registered templates to identify the student.
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string TemplateData { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the biometric device performing the scan.
    /// Used for logging and device tracking.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The session ID if the attendance is for a specific session.
    /// If not provided, the system will attempt to find the active session
    /// for the identified student's current schedule.
    /// </summary>
    public int? SessionId { get; set; }
}
