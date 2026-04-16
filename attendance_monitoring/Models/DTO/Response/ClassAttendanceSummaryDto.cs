namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Lightweight per-session attendance statistics for report views.
/// </summary>
public class SessionAttendanceStatsDto
{
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ScheduleTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int TotalRecords { get; set; }
    public int TotalEnrolled { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Response DTO for section-level attendance report.
/// </summary>
public class ClassAttendanceSummaryDto
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalExcused { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<SessionAttendanceStatsDto> Sessions { get; set; } = new();
}
