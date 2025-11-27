using attendance_monitoring.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services
{
    /// <summary>
    /// Background service for initializing roles asynchronously after application startup
    /// This prevents application startup delays if the database is temporarily unavailable
    /// </summary>
    public class RoleInitializationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoleInitializationBackgroundService> _logger;

        public RoleInitializationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RoleInitializationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting role initialization background service...");

            // Retry configuration
            const int maxRetries = 5;
            const int delayMs = 2000; // 2 seconds between retries

            using var scope = _serviceProvider.CreateScope();
            var roleInitializationService = scope.ServiceProvider
                .GetRequiredService<IRoleInitializationService>();

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await roleInitializationService.InitializeRolesAsync();
                    _logger.LogInformation("Role initialization completed successfully.");
                    return; // Success, exit the service
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Role initialization attempt {Attempt} of {MaxRetries} failed: {Message}",
                        attempt, maxRetries, ex.Message);

                    if (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Retrying role initialization in {DelayMs}ms...", delayMs);
                        try
                        {
                            // Wait for the delay period or until cancellation is requested
                            await Task.Delay(delayMs, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Service is being stopped, exit gracefully
                            _logger.LogInformation("Role initialization service cancelled during retry delay.");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogError(ex, "Role initialization failed after {MaxRetries} attempts. Application may have limited functionality.", maxRetries);
                        return; // Exit the service after all retries are exhausted
                    }
                }
            }
        }
    }
}