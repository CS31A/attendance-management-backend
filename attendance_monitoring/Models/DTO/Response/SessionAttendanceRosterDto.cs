namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// DTO optimized for session attendance roster views.
/// Contains only essential fields for displaying attendance lists.
/// </summary>
public class SessionAttendanceRosterDto
{
    public Guid AttendanceId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public bool IsManualEntry { get; set; }
}
