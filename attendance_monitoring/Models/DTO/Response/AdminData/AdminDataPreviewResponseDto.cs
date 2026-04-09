namespace attendance_monitoring.Models.DTO.Response.AdminData;

public sealed class AdminDataPreviewResponseDto
{
    public bool Success { get; set; }

    public string Entity { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int ReadyRows { get; set; }

    public int DuplicateRows { get; set; }

    public int InvalidRows { get; set; }

    public bool CanImport { get; set; }

    public List<string> Columns { get; set; } = [];

    public List<AdminDataIssueDto> FileIssues { get; set; } = [];

    public List<AdminDataRowResultDto> Rows { get; set; } = [];
}
