namespace attendance_monitoring.Models.DTO.Response;

public class FingerprintEnrollmentSessionResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid EnrollmentSessionId { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public int AssignedSensorFingerprintId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
