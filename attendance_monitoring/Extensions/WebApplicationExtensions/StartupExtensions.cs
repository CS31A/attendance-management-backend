using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for application startup initialization.
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Initializes the application by performing startup tasks.
    /// Currently initializes roles using IRoleInitializationService.
    /// Note: JWT configuration validation occurs earlier during service registration in AuthenticationServiceExtensions.
    /// </summary>
    /// <param name="app">The web application to initialize.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public static async Task InitializeApplicationAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();

        // Data seeder disabled
        // using (var scope = app.Services.CreateScope())
        // {
        //     var dataSeederService = scope.ServiceProvider
        //         .GetRequiredService<IDataSeederService>();
        //
        //     try
        //     {
        //         await dataSeederService.SeedDataAsync();
        //     }
        //     catch (Exception ex)
        //     {
        //         logger.LogError(ex, "Data seeding failed. Application will continue without seed data.");
        //     }
        // }
    }
}
