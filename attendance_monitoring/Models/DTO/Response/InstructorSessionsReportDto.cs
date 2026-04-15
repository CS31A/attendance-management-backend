namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Per-session item in an instructor sessions report.
/// Extends SessionAttendanceStatsDto with section context.
/// </summary>
public class InstructorSessionItemDto
{
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string ScheduleTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int TotalRecords { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Response DTO for instructor-level sessions report.
/// </summary>
public class InstructorSessionsReportDto
{
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public List<InstructorSessionItemDto> Sessions { get; set; } = new();
}
