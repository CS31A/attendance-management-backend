namespace attendance_monitoring.Models.DTO.Response;

public class ErrorResponseDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}