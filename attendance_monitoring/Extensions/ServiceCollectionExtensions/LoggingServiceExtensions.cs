namespace attendance_monitoring.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring logging services.
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Configures application logging with console, debug, and event source providers.
    /// Sets minimum log level to Information and filters Microsoft/System logs to Warning.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddApplicationLogging(this ILoggingBuilder logging)
    {
        // Clear default providers
        logging.ClearProviders();
        
        // Add logging providers
        logging.AddConsole();
        logging.AddDebug();
        logging.AddEventSourceLogger();

        // Configure log levels
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);

        return logging;
    }
}

