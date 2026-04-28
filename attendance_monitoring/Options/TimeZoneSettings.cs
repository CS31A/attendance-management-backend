namespace attendance_monitoring.Options;

public sealed class TimeZoneSettings
{
    public const string SectionName = "TimeZoneSettings";

    public string TimeZoneId { get; set; } = "Asia/Manila";
}
