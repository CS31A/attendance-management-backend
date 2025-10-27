namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Standardized error response structure for API errors
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Indicates if the operation was successful (always false for errors)
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Optional detailed error information (development only)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request path that caused the error
    /// </summary>
    public string? Path { get; set; }
}
