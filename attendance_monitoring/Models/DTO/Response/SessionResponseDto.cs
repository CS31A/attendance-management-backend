namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for session information.
/// </summary>
public class SessionResponseDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public DateTime? AttendanceCutOff { get; set; }
    public string? Description { get; set; }
    public int? ActualRoomId { get; set; }
    public string? ActualRoomName { get; set; }
    public int? StartedBy { get; set; }
    public string? StartedByName { get; set; }
    public int? EndedBy { get; set; }
    public string? EndedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Schedule information
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public string? SectionName { get; set; }
    public string? ScheduledRoomName { get; set; }
}
