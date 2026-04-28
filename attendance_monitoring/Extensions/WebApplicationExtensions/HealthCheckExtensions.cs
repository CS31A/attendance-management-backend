using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for configuring health check endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Maps health check endpoints for liveness and readiness probes.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness probe - simple check that the application is running
        // No health checks are executed, just returns 200 OK
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Don't run any health checks
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow
                });
                await context.Response.WriteAsync(result);
            }
        });

        // Readiness probe - checks if the application is ready to serve traffic
        // Runs all health checks tagged with "ready"
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        data = e.Value.Data
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        // Detailed health check endpoint - includes all health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        exception = e.Value.Exception?.Message,
                        data = e.Value.Data,
                        tags = e.Value.Tags
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
