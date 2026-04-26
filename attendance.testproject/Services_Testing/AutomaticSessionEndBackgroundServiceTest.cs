using attendance_monitoring.IServices;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Services_Testing;

public class AutomaticSessionEndBackgroundServiceTest
{
    [Fact]
    public async Task StartAsync_DoesNotScan_WhenDisabled()
    {
        var scopeFactory = CreateScopeFactory(new Mock<IAutomaticSessionEndService>().Object);
        var service = new AutomaticSessionEndBackgroundService(
            scopeFactory,
            Options.Create(new SessionAutoEndOptions { Enabled = false }),
            NullLogger<AutomaticSessionEndBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        var automaticEndService = Mock.Get(scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IAutomaticSessionEndService>());
        automaticEndService.Verify(scan => scan.AutoEndExpiredSessionsAsync(), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ScansImmediately_WhenEnabled()
    {
        var scanned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var automaticEndService = new Mock<IAutomaticSessionEndService>();
        automaticEndService
            .Setup(service => service.AutoEndExpiredSessionsAsync())
            .ReturnsAsync(1)
            .Callback(() => scanned.TrySetResult());

        var service = new AutomaticSessionEndBackgroundService(
            CreateScopeFactory(automaticEndService.Object),
            Options.Create(new SessionAutoEndOptions { Enabled = true, ScanIntervalMinutes = 1 }),
            NullLogger<AutomaticSessionEndBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await scanned.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        automaticEndService.Verify(scan => scan.AutoEndExpiredSessionsAsync(), Times.AtLeastOnce);
    }

    private static IServiceScopeFactory CreateScopeFactory(IAutomaticSessionEndService automaticEndService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(automaticEndService);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}
