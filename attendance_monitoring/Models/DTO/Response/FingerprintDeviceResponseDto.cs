namespace attendance_monitoring.Models.DTO.Response;

public class FingerprintDeviceResponseDto
{
    public Guid Id { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
