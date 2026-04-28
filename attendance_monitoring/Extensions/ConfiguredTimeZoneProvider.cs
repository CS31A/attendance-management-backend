using attendance_monitoring.Options;

namespace attendance_monitoring.Extensions;

/// <summary>
/// TimeProvider wrapper that provides local time in a configured timezone.
/// </summary>
public sealed class ConfiguredTimeZoneProvider
{
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeProvider _innerProvider;

    public ConfiguredTimeZoneProvider(TimeZoneSettings settings)
        : this(settings, TimeProvider.System)
    {
    }

    public ConfiguredTimeZoneProvider(TimeZoneSettings settings, TimeProvider innerProvider)
        : this(settings, innerProvider, TimeZoneInfo.FindSystemTimeZoneById)
    {
    }

    public ConfiguredTimeZoneProvider(
        TimeZoneSettings settings,
        TimeProvider innerProvider,
        Func<string, TimeZoneInfo> timeZoneResolver)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(innerProvider);
        ArgumentNullException.ThrowIfNull(timeZoneResolver);

        _innerProvider = innerProvider;

        var configuredTimeZoneId = string.IsNullOrWhiteSpace(settings.TimeZoneId)
            ? TimeZoneInfo.Local.Id
            : settings.TimeZoneId;

        try
        {
            _timeZone = timeZoneResolver(configuredTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to system local time if the configured timezone doesn't exist
            _timeZone = TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            // Fallback to system local time if timezone data is corrupt/invalid
            _timeZone = TimeZoneInfo.Local;
        }
    }

    /// <summary>
    /// Gets the current UTC time from the inner provider as DateTimeOffset.
    /// </summary>
    public DateTimeOffset GetUtcNow()
    {
        return _innerProvider.GetUtcNow();
    }

    /// <summary>
    /// The configured timezone used for local time conversions.
    /// </summary>
    public TimeZoneInfo TimeZone => _timeZone;

    /// <summary>
    /// Gets the current local time in the configured timezone as DateTime.
    /// Returns Kind = DateTimeKind.Local so that EF Core/SQL Server preserves the
    /// timezone context rather than treating the value as UTC on retrieval.
    /// </summary>
    public DateTime GetLocalNow()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(_innerProvider.GetUtcNow().UtcDateTime, _timeZone);
        return DateTime.SpecifyKind(local, DateTimeKind.Local);
    }
}
