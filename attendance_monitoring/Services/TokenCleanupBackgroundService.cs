using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Background service that periodically cleans up expired refresh tokens from the database.
/// This service helps maintain database performance and removes security risks from expired tokens.
/// </summary>
public class TokenCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<TokenCleanupBackgroundService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupIntervalMinutes = configuration.GetValue("TokenCleanup:IntervalMinutes", 60); // Default 1 hour
        var batchSize = configuration.GetValue("TokenCleanup:BatchSize", 1000); // Default batch size
        var cleanupInterval = TimeSpan.FromMinutes(cleanupIntervalMinutes);

        logger.LogInformation(
            "Token Cleanup Service started. Cleanup interval: {CleanupInterval} minutes, Batch size: {BatchSize}",
            cleanupIntervalMinutes, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(batchSize, stoppingToken).ConfigureAwait(false);
                
                // Wait for the configured interval before next cleanup
                await Task.Delay(cleanupInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Service is being stopped
                logger.LogInformation("Token Cleanup Service is being stopped.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while cleaning up expired tokens.");
                
                // Wait for half the interval before retrying to avoid rapid retries
                await Task.Delay(TimeSpan.FromMinutes(cleanupIntervalMinutes / 2), stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Token Cleanup Service has stopped.");
    }

    /// <summary>
    /// Performs the actual cleanup of expired tokens in batches
    /// </summary>
    /// <param name="batchSize">Number of tokens to process in each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task PerformCleanupAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var totalRemoved = 0;
        var currentDateTime = DateTime.UtcNow;
        
        logger.LogDebug("Starting token cleanup process at {CurrentTime}", currentDateTime);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Find expired tokens in batches
            var expiredTokens = await context.RefreshTokens
                .Where(rt => rt.ExpiresAt < currentDateTime || 
                           (rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < currentDateTime.AddDays(-30)))
                .OrderBy(rt => rt.ExpiresAt) // Ensure predictable ordering
                .Take(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (expiredTokens.Count == 0)
            {
                // No more expired tokens to clean up
                break;
            }

            try
            {
                // Remove the expired tokens
                context.RefreshTokens.RemoveRange(expiredTokens);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var batchCount = expiredTokens.Count;
                totalRemoved += batchCount;

                logger.LogDebug("Removed batch of {BatchCount} expired tokens", batchCount);

                // Log details about what types of tokens were removed
                var expiredCount = expiredTokens.Count(t => t.ExpiresAt < currentDateTime);
                var revokedCount = expiredTokens.Count(t => t.IsRevoked && t.RevokedAt != null && t.RevokedAt < currentDateTime.AddDays(-30));

                if (expiredCount > 0)
                {
                    logger.LogDebug("Removed {ExpiredCount} naturally expired tokens", expiredCount);
                }
                if (revokedCount > 0)
                {
                    logger.LogDebug("Removed {RevokedCount} old revoked tokens (audit cleanup)", revokedCount);
                }

                // If we processed less than the batch size, we're done
                if (expiredTokens.Count < batchSize)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove batch of {BatchCount} expired tokens", expiredTokens.Count);
                throw; // Re-throw to be handled by the outer exception handler
            }
        }

        if (totalRemoved > 0)
        {
            logger.LogInformation(
                "Token cleanup completed. Removed {TotalCount} expired refresh tokens.", 
                totalRemoved);
        }
        else
        {
            logger.LogDebug("Token cleanup completed. No expired tokens found.");
        }
    }

    /// <summary>
    /// Provides health check information about the service
    /// </summary>
    public Task<string> GetHealthStatusAsync()
    {
        return Task.FromResult("Token Cleanup Service is running");
    }
}
