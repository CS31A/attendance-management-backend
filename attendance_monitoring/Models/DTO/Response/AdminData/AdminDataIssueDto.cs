namespace attendance_monitoring.Models.DTO.Response.AdminData;

public sealed class AdminDataIssueDto
{
    public int? RowNumber { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Field { get; set; }
}
