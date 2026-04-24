namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for student-specific attendance history with statistics.
/// </summary>
public class StudentAttendanceHistoryDto
{
    public int StudentId { get; set; }
    public Guid? StudentUuid { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendancePercentage { get; set; }
    public List<AttendanceRecordResponseDto> AttendanceRecords { get; set; } = new();
}
