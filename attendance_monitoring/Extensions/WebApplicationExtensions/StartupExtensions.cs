using attendance_monitoring.IServices;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for application startup initialization.
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Initializes the application by performing startup tasks.
    /// Currently initializes roles using IRoleInitializationService.
    /// </summary>
    /// <param name="app">The web application to initialize.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public static async Task InitializeApplicationAsync(this WebApplication app)
    {
        // Initialize roles
        using (var scope = app.Services.CreateScope())
        {
            var roleInitializationService = scope.ServiceProvider
                .GetRequiredService<IRoleInitializationService>();
            await roleInitializationService.InitializeRolesAsync();
        }
    }
}

