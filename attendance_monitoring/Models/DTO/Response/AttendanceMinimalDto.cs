namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Minimal DTO for attendance record data without navigation properties.
/// Optimized for lightweight lookups and existence checks.
/// </summary>
public class AttendanceMinimalDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public int? QrCodeId { get; set; }
}
