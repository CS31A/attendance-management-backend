using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Text.Json.Serialization;

namespace attendance_monitoring.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring response handling (compression, JSON serialization, CORS).
/// </summary>
public static class ResponseHandlingExtensions
{
    /// <summary>
    /// Adds response compression and JSON serialization configuration.
    /// Configures Gzip compression for JSON responses and ignores null values in JSON output.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResponseHandling(this IServiceCollection services)
    {
        // Add Response Compression with Gzip - Selective application
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true; // Enable compression for HTTPS requests
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = new[] { "application/json", "text/json" };
        });

        // Configure Gzip compression level
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal; // Best compression ratio
        });

        // Configure JSON serialization options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Ignore null values in JSON responses for cleaner output
                options.JsonSerializerOptions.DefaultIgnoreCondition = 
                    JsonIgnoreCondition.WhenWritingNull;
            });

        return services;
    }

    /// <summary>
    /// Adds CORS policy to allow frontend access.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="policyName">The name of the CORS policy (default: "AllowFrontend").</param>
    /// <param name="frontendOrigin">The frontend origin URL (default: "http://localhost:5173").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        string policyName = "AllowFrontend",
        string frontendOrigin = "http://localhost:5173")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.WithOrigins(frontendOrigin) // Frontend origin
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // If you need to send cookies or authorization headers
            });
        });

        return services;
    }
}

