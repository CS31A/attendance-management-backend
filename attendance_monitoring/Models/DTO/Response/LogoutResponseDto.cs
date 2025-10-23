namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Unified response DTO for all logout operations
/// </summary>
public class LogoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}