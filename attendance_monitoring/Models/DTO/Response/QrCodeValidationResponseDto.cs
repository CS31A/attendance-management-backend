namespace attendance_monitoring.Models.DTO.Response;

public class QrCodeValidationResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? QrCodeId { get; set; }
    public int? ScheduleId { get; set; }
    public int? SectionId { get; set; }
    public int? ActualRoomId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingUsage { get; set; }
    public bool? AttendanceRecorded { get; set; }

    // Context information for the client
    public string? ScheduleTitle { get; set; }
    public string? SectionName { get; set; }
    public string? RoomName { get; set; }
    public string? SubjectName { get; set; }
    public string? InstructorName { get; set; }
}