using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request DTO for registering a fingerprint for a student.
/// </summary>
public class RegisterFingerprint
{
    /// <summary>
    /// The student ID to register the fingerprint for.
    /// </summary>
    [Required]
    public int StudentId { get; set; }

    /// <summary>
    /// The fingerprint template data from the biometric device.
    /// This is typically a base64-encoded or hex-encoded representation
    /// of the fingerprint minutiae template.
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string TemplateData { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the biometric device.
    /// Used to track which device registered the fingerprint.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The sensor-specific fingerprint ID from the biometric device.
    /// This is the internal ID assigned by the sensor/device.
    /// </summary>
    [Required]
    public int SensorFingerprintId { get; set; }
}
