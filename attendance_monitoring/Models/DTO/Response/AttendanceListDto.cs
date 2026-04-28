namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Lightweight DTO for attendance record listing with minimal required fields.
/// Optimized for performance with database projections.
/// </summary>
public class AttendanceListDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
