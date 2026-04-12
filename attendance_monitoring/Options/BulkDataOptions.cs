namespace attendance_monitoring.Options;

public sealed class BulkDataOptions
{
    public const string SectionName = "BulkData";

    public int MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxRows { get; set; } = 1000;

    public int MaxPreviewRows { get; set; } = 250;

    public int MaxIssues { get; set; } = 500;
}
