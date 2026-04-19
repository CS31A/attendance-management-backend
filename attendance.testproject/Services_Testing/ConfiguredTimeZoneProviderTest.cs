using attendance_monitoring.Extensions;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.Options;
using Microsoft.Extensions.DependencyInjection;

namespace attendance.testproject.Services_Testing;

public sealed class ConfiguredTimeZoneProviderTest
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 4, 19, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WhenTimeZoneIdIsInvalid_FallsBackToSystemLocalTimeZone()
    {
        var provider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = "Invalid/Timezone" },
            new FixedTimeProvider(FixedUtcNow));

        var expectedLocal = TimeZoneInfo.ConvertTimeFromUtc(FixedUtcNow.UtcDateTime, TimeZoneInfo.Local);

        Assert.Equal(FixedUtcNow, provider.GetUtcNow());
        Assert.Equal(expectedLocal, provider.GetLocalNow());
    }

    [Fact]
    public void Constructor_WhenTimeZoneDataIsInvalid_FallsBackToSystemLocalTimeZone()
    {
        var provider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = "Asia/Manila" },
            new FixedTimeProvider(FixedUtcNow),
            _ => throw new InvalidTimeZoneException("Corrupt timezone data"));

        var expectedLocal = TimeZoneInfo.ConvertTimeFromUtc(FixedUtcNow.UtcDateTime, TimeZoneInfo.Local);

        Assert.Equal(expectedLocal, provider.GetLocalNow());
    }

    [Fact]
    public void AddApplicationServices_WhenTimeZoneConfigurationIsMissing_FallsBackToSystemLocalTimeZone()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedUtcNow));
        services.AddApplicationServices();

        using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<ConfiguredTimeZoneProvider>();

        var expectedLocal = TimeZoneInfo.ConvertTimeFromUtc(FixedUtcNow.UtcDateTime, TimeZoneInfo.Local);
        Assert.Equal(expectedLocal, provider.GetLocalNow());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}