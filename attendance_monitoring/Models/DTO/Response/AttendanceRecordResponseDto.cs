namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for a comprehensive attendance record.
/// </summary>
internal interface IIdempotentAttendanceRetryResult;

public class AttendanceRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public Guid? QrCodeId { get; set; }
    public DateTime CheckInTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Schedule and session details
    public Guid ScheduleId { get; set; }
    public string ScheduleTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
}
