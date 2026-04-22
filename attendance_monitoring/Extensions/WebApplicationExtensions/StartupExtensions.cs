using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            if (dbContext.Database.IsRelational() && databaseOptions.ApplyMigrationsAtStartup)
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                var pendingMigrationList = pendingMigrations.ToArray();

                if (pendingMigrationList.Length > 0)
                {
                    logger.LogInformation(
                        "Applying pending EF Core migrations at startup: {PendingMigrations}.",
                        pendingMigrationList);

                    await dbContext.Database.MigrateAsync();

                    logger.LogInformation(
                        "Applied pending EF Core migrations at startup: {PendingMigrations}.",
                        pendingMigrationList);
                }
            }

            var dataSeederService = scope.ServiceProvider
                .GetRequiredService<IDataSeederService>();

            try
            {
                await dataSeederService.SeedDataAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Data seeding failed. Application will continue without seed data.");
                // Don't throw - allow application to start even if seeding fails
            }
        }
    }
}
