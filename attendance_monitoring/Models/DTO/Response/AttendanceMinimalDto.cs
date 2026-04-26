namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Minimal DTO for attendance record data without navigation properties.
/// Optimized for lightweight lookups and existence checks.
/// </summary>
public class AttendanceMinimalDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public Guid? QrCodeId { get; set; }
}
