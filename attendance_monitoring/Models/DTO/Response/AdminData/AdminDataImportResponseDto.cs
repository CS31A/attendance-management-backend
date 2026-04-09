namespace attendance_monitoring.Models.DTO.Response.AdminData;

public sealed class AdminDataImportResponseDto
{
    public bool Success { get; set; }

    public string Entity { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int CreatedRows { get; set; }

    public int SkippedDuplicateRows { get; set; }

    public int FailedRows { get; set; }

    public List<AdminDataIssueDto> FileIssues { get; set; } = [];

    public List<AdminDataRowResultDto> Rows { get; set; } = [];
}
