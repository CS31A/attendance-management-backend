using attendance_monitoring.Extensions;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using System.Diagnostics;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for configuring the middleware pipeline.
/// </summary>
public static class MiddlewarePipelineExtensions
{
    /// <summary>
    /// Adds selective response compression middleware.
    /// Only compresses GET requests for specific API endpoints.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseSelectiveResponseCompression(this WebApplication app)
    {
        // Add SELECTIVE Response Compression - Only for specific GET endpoints
        app.UseWhen(
            context =>
            {
                var method = context.Request.Method;
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

                // Only apply compression to GET requests
                if (method != "GET") return false;

                // Check for specific endpoints that should be compressed
                return path.StartsWith("/api/classrooms") ||
                       path.StartsWith("/api/course") ||
                       path.StartsWith("/api/instructors") ||
                       path.StartsWith("/api/schedules") ||
                       path.StartsWith("/api/sections") ||
                       path.StartsWith("/api/students") ||
                       path.StartsWith("/api/subjects");
            },
            appBuilder => appBuilder.UseResponseCompression()
        );

        return app;
    }

    /// <summary>
    /// Adds performance monitoring middleware.
    /// Logs response time and compression information for API endpoints.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UsePerformanceMonitoring(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var stopwatch = Stopwatch.StartNew();

            // Set up response callback to capture timing after response is complete
            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;

                // Add debug headers for client inspection (before response starts)
                context.Response.Headers["X-Response-Time"] = $"{responseTime}ms";

                // Check compression headers (already applied by compression middleware)
                var isCompressed = context.Response.Headers.ContainsKey("Content-Encoding");
                if (isCompressed)
                {
                    var compressionType = context.Response.Headers["Content-Encoding"].ToString();
                    context.Response.Headers["X-Compression"] = compressionType;
                }

                return Task.CompletedTask;
            });

            await next();

            // Log performance metrics for API endpoints with successful responses
            if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
            {
                var responseTime = stopwatch.ElapsedMilliseconds;
                var isCompressed = context.Response.Headers.ContainsKey("Content-Encoding");
                var compressionType = isCompressed ? context.Response.Headers["Content-Encoding"].ToString() : "none";
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                var contentType = context.Response.ContentType ?? "";

                logger.LogInformation(
                    "PERF: {Endpoint} | {ResponseTime}ms | Compressed: {IsCompressed} ({CompressionType}) | Content-Type: {ContentType}",
                    endpoint, responseTime, isCompressed, compressionType, contentType
                );
            }
        });

        return app;
    }

    /// <summary>
    /// Configures development-only tools (Swagger UI, Scalar API reference).
    /// Only enabled when running in Development environment.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseDevelopmentTools(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance Monitoring API");
            });
            app.MapOpenApi();

            // Configure Scalar to use the Swagger-generated OpenAPI document (includes MVC controller endpoints)
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Attendance Monitoring API")
                    .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            });
        }

        return app;
    }

    /// <summary>
    /// Configures the core middleware pipeline.
    /// Includes HTTPS redirection, CORS, authentication, authorization, and controller mapping.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <param name="corsPolicy">The CORS policy name to use (default: "AllowFrontend").</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseCorePipeline(
        this WebApplication app,
        string corsPolicy = "AllowFrontend")
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(corsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Map SignalR hubs
        app.MapSignalRHubs();

        return app;
    }
}

