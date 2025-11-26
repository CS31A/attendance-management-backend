namespace attendance_monitoring.Models.DTO.Response;

public record NotificationDto
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = "Info";  // Info, Success, Warning, Error
    public string Category { get; init; } = string.Empty;  // QrCode, Session, Attendance
    public object? Metadata { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
