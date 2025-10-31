using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

public class BlacklistedTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlacklistedTokenCleanupService> _logger;

    public BlacklistedTokenCleanupService(IServiceProvider serviceProvider, ILogger<BlacklistedTokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    #region Background Service Methods

    #region ExecuteAsync
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blacklisted Token Cleanup Service running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a scope to resolve dependencies
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Remove expired blacklisted tokens
                var expiredTokens = await context.BlacklistedTokens
                    .AsNoTracking()
                    .Where(bt => bt.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken).ConfigureAwait(false);

                if (expiredTokens.Any())
                {
                    context.BlacklistedTokens.RemoveRange(expiredTokens);
                    await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogInformation("Removed {Count} expired blacklisted tokens.", expiredTokens.Count);
                }

                // Wait for 1 hour before next cleanup
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up blacklisted tokens.");
                // Wait for 1 hour before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Blacklisted Token Cleanup Service is stopping.");
    }
    #endregion

    #endregion
}