using attendance_monitoring.Extensions;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using System.Diagnostics;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for configuring the middleware pipeline.
/// </summary>
public static class MiddlewarePipelineExtensions
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private const string CorrelationItemKey = "CorrelationId";

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
            var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationHeaderName].ToString());

            context.TraceIdentifier = correlationId;
            context.Items[CorrelationItemKey] = correlationId;
            context.Response.Headers[CorrelationHeaderName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            using (logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await next();
            }
        });

        app.Use(async (context, next) =>
        {
            var stopwatch = Stopwatch.StartNew();
            var endpointGroup = GetEndpointGroup(context.Request.Path);

            // Set up response callback to capture timing after response is complete
            context.Response.OnStarting(() =>
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }

                var responseTime = stopwatch.Elapsed.TotalMilliseconds;

                // Add debug headers for client inspection (before response starts)
                context.Response.Headers["X-Response-Time"] = $"{Math.Round(responseTime, 2)}ms";

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

            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var responseTime = stopwatch.Elapsed.TotalMilliseconds;
                var isCompressed = context.Response.Headers.ContainsKey("Content-Encoding");
                var compressionType = isCompressed ? context.Response.Headers["Content-Encoding"].ToString() : "none";
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                var contentType = context.Response.ContentType ?? "";
                var correlationId = context.TraceIdentifier;
                var statusCode = context.Response.StatusCode;

                if (endpointGroup is not null)
                {
                    var telemetry = context.RequestServices.GetRequiredService<RequestReliabilityTelemetry>();
                    telemetry.RecordRequest(responseTime, endpointGroup, context.Request.Method, statusCode);

                    if (statusCode >= StatusCodes.Status400BadRequest)
                    {
                        telemetry.RecordError(endpointGroup, context.Request.Method, statusCode);
                    }

                    logger.LogInformation(
                        "RequestComplete {EndpointGroup} {StatusCode} {ElapsedMilliseconds} {CorrelationId}",
                        endpointGroup,
                        statusCode,
                        responseTime,
                        correlationId);
                }
                else
                {
                    if (statusCode >= StatusCodes.Status500InternalServerError)
                    {
                        logger.LogWarning(
                            "PERF: {Endpoint} | {StatusCode} | {ResponseTime}ms | CorrelationId: {CorrelationId} | Compressed: {IsCompressed} ({CompressionType}) | Content-Type: {ContentType}",
                            endpoint,
                            statusCode,
                            responseTime,
                            correlationId,
                            isCompressed,
                            compressionType,
                            contentType);
                    }
                    else
                    {
                        logger.LogInformation(
                            "PERF: {Endpoint} | {StatusCode} | {ResponseTime}ms | CorrelationId: {CorrelationId} | Compressed: {IsCompressed} ({CompressionType}) | Content-Type: {ContentType}",
                            endpoint,
                            statusCode,
                            responseTime,
                            correlationId,
                            isCompressed,
                            compressionType,
                            contentType);
                    }
                }
            }
        });

        return app;
    }

    private static string ResolveCorrelationId(string? inboundCorrelationId)
    {
        if (IsValidCorrelationId(inboundCorrelationId))
        {
            return inboundCorrelationId!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
        {
            return false;
        }

        foreach (var character in correlationId)
        {
            if (!char.IsLetterOrDigit(character) && character is not '.' and not '_' and not '-')
            {
                return false;
            }
        }

        return true;
    }

    private static string? GetEndpointGroup(PathString path)
    {
        if (path.StartsWithSegments("/api/account", StringComparison.OrdinalIgnoreCase))
        {
            return "auth";
        }

        if (path.StartsWithSegments("/api/attendance", StringComparison.OrdinalIgnoreCase))
        {
            return "attendance";
        }

        if (path.StartsWithSegments("/api/qrcode", StringComparison.OrdinalIgnoreCase))
        {
            return "qr";
        }

        return null;
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
        // Only use HTTPS redirection in development
        // In production, the reverse proxy (AWS EB/nginx) handles SSL termination
        if (app.Environment.IsDevelopment())
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
