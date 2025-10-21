namespace attendance_monitoring.Models.DTO.Response;

public class QrCodeGenerationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string QrHash { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty; // The actual QR code content/URL
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? MaxUsage { get; set; }
    public int QrCodeId { get; set; }
}