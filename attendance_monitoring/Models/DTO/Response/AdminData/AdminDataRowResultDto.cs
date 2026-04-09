namespace attendance_monitoring.Models.DTO.Response.AdminData;

public sealed class AdminDataRowResultDto
{
    public int RowNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<AdminDataIssueDto> Issues { get; set; } = [];
}
