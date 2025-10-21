namespace attendance_monitoring.Models.DTO.Response;

public class QrCodeResponseDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int SectionId { get; set; }
    public int ActualRoomId { get; set; }
    public string QrHash { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int? MaxUsage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related entity information
    public string? ScheduleTitle { get; set; }
    public string? SectionName { get; set; }
    public string? ActualRoomName { get; set; }
    public string? SubjectName { get; set; }
    public string? InstructorName { get; set; }
}