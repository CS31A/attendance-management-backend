namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for fingerprint registration operations.
/// </summary>
public class FingerprintRegistrationResponseDto
{
    /// <summary>
    /// Indicates if the registration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the newly registered fingerprint.
    /// </summary>
    public int? FingerprintId { get; set; }

    /// <summary>
    /// The student ID the fingerprint was registered for.
    /// </summary>
    public int? StudentId { get; set; }

    /// <summary>
    /// The student's name.
    /// </summary>
    public string? StudentName { get; set; }
}
