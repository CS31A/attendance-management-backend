namespace attendance_monitoring.Models.DTO.Response.AdminData;

public sealed class AdminDataFileDto
{
    public required byte[] Content { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }
}
