namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Lightweight projected row for session-based report queries.
/// </summary>
public sealed class SessionReportRowDto
{
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int TotalRecords { get; set; }
    public int TotalEnrolled { get; set; }
}
