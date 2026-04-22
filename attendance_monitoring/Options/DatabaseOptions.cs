namespace attendance_monitoring.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsAtStartup { get; set; } = true;
}
