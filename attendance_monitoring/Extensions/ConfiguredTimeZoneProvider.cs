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
    {
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to system local time if the configured timezone doesn't exist
            _timeZone = TimeZoneInfo.Local;
        }
        _innerProvider = innerProvider;
    }

    /// <summary>
    /// Gets the current UTC time from the inner provider as DateTimeOffset.
    /// </summary>
    public DateTimeOffset GetUtcNow()
    {
        return _innerProvider.GetUtcNow();
    }

    /// <summary>
    /// Gets the current local time in the configured timezone as DateTime.
    /// </summary>
    public DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(_innerProvider.GetUtcNow().UtcDateTime, _timeZone);
    }
}
