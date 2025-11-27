using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
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
    /// Reads allowed origins from configuration (CorsSettings:AllowedOrigins).
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="policyName">The name of the CORS policy (default: "AllowFrontend").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName = "AllowFrontend")
    {
        // Read allowed origins from configuration
        var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string>();
        
        // Parse origins (supports semicolon-separated list)
        var origins = string.IsNullOrWhiteSpace(allowedOrigins)
            ? new[] { "http://localhost:5173" } // Default fallback for development
            : allowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.WithOrigins(origins) // Frontend origins from configuration
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for cookies and authorization headers
            });
        });

        return services;
    }
}

