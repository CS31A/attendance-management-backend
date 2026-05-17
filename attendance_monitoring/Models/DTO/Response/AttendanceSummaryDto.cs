using attendance_monitoring.Constants;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for statistical summary of attendance data.
/// </summary>
public class AttendanceSummaryDto
{
    public int TotalSessions { get; set; }
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalExcused { get; set; }
    public decimal AttendanceRate { get; set; }
    public string? AverageCheckInTime { get; set; }
    public string MostFrequentStatus { get; set; } = AttendanceStatusConstants.Present;
}
