using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
// Alternative implementation would also need:
// using attendance_monitoring.IRepository;

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
        
        logger.LogDebug("Starting token cleanup process");

        while (!cancellationToken.IsCancellationRequested)
        {
            var currentDateTime = DateTime.UtcNow; // Update time for each batch to improve consistency
            
            // Count expired tokens before deletion for logging purposes
            var tokensToDeleteCount = await context.RefreshTokens
                .Where(rt => rt.ExpiresAt < currentDateTime || 
                           (rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < currentDateTime.AddDays(-30)))
                .OrderBy(rt => rt.Id) // Ensure predictable ordering
                .Take(batchSize)
                .CountAsync(cancellationToken);

            if (tokensToDeleteCount == 0)
            {
                // No more expired tokens to clean up
                break;
            }

            try
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try 
                {
                    // Delete expired tokens in bulk using raw SQL for better performance
                    var deletedCount = await context.Database.ExecuteSqlRawAsync(
                        "DELETE TOP({2}) FROM RefreshTokens WHERE (ExpiresAt < {0} OR (IsRevoked = 1 AND RevokedAt IS NOT NULL AND RevokedAt < {1}))",
                        currentDateTime, 
                        currentDateTime.AddDays(-30), 
                        batchSize,
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    totalRemoved += deletedCount;

                    logger.LogDebug("Removed batch of {BatchCount} expired tokens", deletedCount);

                    // For better logging, we need to separately determine how many were expired vs revoked
                    // Note: In a production system, for better performance you might want to skip this detail
                    var expiredCount = await context.RefreshTokens
                        .Where(rt => rt.ExpiresAt < currentDateTime && rt.ExpiresAt >= currentDateTime.AddDays(-1)) // Approximation
                        .OrderBy(rt => rt.Id) // Ensure predictable ordering
                        .CountAsync(cancellationToken);
                    var revokedCount = await context.RefreshTokens
                        .Where(rt => rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < currentDateTime.AddDays(-30) && rt.RevokedAt >= currentDateTime.AddDays(-31)) // Approximation
                        .OrderBy(rt => rt.Id) // Ensure predictable ordering
                        .CountAsync(cancellationToken);

                    if (expiredCount > 0)
                    {
                        logger.LogDebug("Removed {ExpiredCount} naturally expired tokens", expiredCount);
                    }
                    if (revokedCount > 0)
                    {
                        logger.LogDebug("Removed {RevokedCount} old revoked tokens (audit cleanup)", revokedCount);
                    }

                    // If we processed less than the batch size, we're likely done with all expired tokens
                    if (deletedCount < batchSize)
                    {
                        break;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove batch of expired tokens");
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

    /* ALTERNATIVE IMPLEMENTATION - Entity Framework LINQ Approach (Database Agnostic)
    /// <summary>
    /// Performs the actual cleanup of expired tokens in batches using Entity Framework LINQ
    /// This approach is database-agnostic and follows the repository pattern
    /// </summary>
    /// <param name="batchSize">Number of tokens to process in each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task PerformCleanupAsyncEF(int batchSize, CancellationToken cancellationToken)
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

            if (!expiredTokens.Any())
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
    
    // ALTERNATIVE WITH REPOSITORY PATTERN
    // If using the repository pattern with the new methods added to IRefreshTokenRepository:
    private async Task PerformCleanupAsyncRepository(int batchSize, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        
        var totalRemoved = 0;
        var currentDateTime = DateTime.UtcNow;
        
        logger.LogDebug("Starting token cleanup process at {CurrentTime}", currentDateTime);

        while (!cancellationToken.IsCancellationRequested)
        {
            var expiredTokens = await refreshTokenRepository
                .GetExpiredTokensAsync(currentDateTime, batchSize, cancellationToken)
                .ConfigureAwait(false);

            if (!expiredTokens.Any())
            {
                // No more expired tokens to clean up
                break;
            }

            try
            {
                var removedCount = await refreshTokenRepository
                    .RemoveTokensAsync(expiredTokens, cancellationToken)
                    .ConfigureAwait(false);
                    
                totalRemoved += removedCount;
                logger.LogDebug("Removed batch of {BatchCount} expired tokens", removedCount);

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
                logger.LogError(ex, "Failed to remove batch of expired tokens");
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
    */

    /// <summary>
    /// Provides health check information about the service
    /// </summary>
    public Task<string> GetHealthStatusAsync()
    {
        return Task.FromResult("Token Cleanup Service is running");
    }
}
