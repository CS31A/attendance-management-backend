namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for QR code information.
/// </summary>
public class QrCodeResponseDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string QrHash { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int? MaxUsage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Session information (from Session entity)
    public Guid? ScheduleId { get; set; }
    public DateTime? SessionDate { get; set; }
    public string? SessionStatus { get; set; }

    // Related entity information (from Session -> Schedule)
    public string? ScheduleTitle { get; set; }
    public string? SectionName { get; set; }
    public string? ActualRoomName { get; set; }
    public string? SubjectName { get; set; }
    public string? InstructorName { get; set; }
}
