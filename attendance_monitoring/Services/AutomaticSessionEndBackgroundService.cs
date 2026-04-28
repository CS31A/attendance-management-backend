using attendance_monitoring.IServices;
using attendance_monitoring.Options;
using Microsoft.Extensions.Options;

namespace attendance_monitoring.Services;

public sealed class AutomaticSessionEndBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<SessionAutoEndOptions> options,
    ILogger<AutomaticSessionEndBackgroundService> logger) : BackgroundService
{
    private readonly SessionAutoEndOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Automatic session ending is disabled.");
            return;
        }

        logger.LogInformation(
            "Automatic session ending started with scan interval {ScanInterval}.",
            _options.ScanInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAutomaticSessionEndService>();
                await service.AutoEndExpiredSessionsAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automatic session ending scan failed.");
            }

            await Task.Delay(_options.ScanInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}
