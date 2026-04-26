namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Per-session item in an instructor sessions report.
/// Inherits from SessionAttendanceStatsDto and adds section context.
/// </summary>
public class InstructorSessionItemDto : SessionAttendanceStatsDto
{
    public string SectionName { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for instructor-level sessions report.
/// </summary>
public class InstructorSessionsReportDto
{
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public List<InstructorSessionItemDto> Sessions { get; set; } = new();
}
