namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for a student's attendance record in a session attendance overview.
/// </summary>
public class StudentAttendanceRecordDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public Guid? AttendanceRecordId { get; set; }
    public string Status { get; set; } = "Absent";
    public DateTime? CheckInTime { get; set; }
    public bool IsManualEntry { get; set; }
}

/// <summary>
/// Response DTO for session-specific attendance overview.
/// </summary>
public class SessionAttendanceDto
{
    public Guid SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public Guid ScheduleId { get; set; }
    public string ScheduleTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<StudentAttendanceRecordDto> AttendanceRecords { get; set; } = new();
}
