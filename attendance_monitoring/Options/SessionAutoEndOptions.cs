namespace attendance_monitoring.Options;

public sealed class SessionAutoEndOptions
{
    public const string SectionName = "SessionAutoEnd";

    public bool Enabled { get; set; } = true;

    public int GraceMinutes { get; set; } = 15;

    public int ScanIntervalMinutes { get; set; } = 5;

    public TimeSpan GracePeriod => TimeSpan.FromMinutes(Math.Max(0, GraceMinutes));

    public TimeSpan ScanInterval => TimeSpan.FromMinutes(Math.Max(1, ScanIntervalMinutes));
}
