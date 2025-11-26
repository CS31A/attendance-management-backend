using attendance_monitoring.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace attendance_monitoring.Extensions;

/// <summary>
/// Extension methods for configuring SignalR services
/// </summary>
public static class SignalRServiceExtensions
{
    /// <summary>
    /// Adds SignalR with JWT authentication support
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            // Enable detailed errors in development (configure based on environment in Program.cs)
            options.EnableDetailedErrors = false;

            // Client timeout - how long to wait before considering client disconnected
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

            // Keep-alive interval - how often to send keep-alive messages
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            // Maximum message size (1MB default, adjust if needed)
            options.MaximumReceiveMessageSize = 1024 * 1024;
        });

        return services;
    }

    /// <summary>
    /// Maps SignalR hub endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapSignalRHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/notificationHub");
        return endpoints;
    }

    /// <summary>
    /// Configures SignalR-specific authentication
    /// </summary>
    public static void ConfigureSignalRAuthentication(JwtBearerEvents events)
    {
        var onMessageReceived = events.OnMessageReceived;

        events.OnMessageReceived = context =>
        {
            // Extract token from query string for SignalR connections
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                // Read the token from the query string
                context.Token = accessToken;
            }

            return onMessageReceived(context);
        };
    }
}
