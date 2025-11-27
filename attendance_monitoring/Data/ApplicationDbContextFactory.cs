using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace attendance_monitoring.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext to enable EF Core tools to create instances
/// of the context for migrations and other design-time operations.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new instance of the ApplicationDbContext for design-time operations.
    /// This method is called by EF Core tools (like Add-Migration, Update-Database) 
    /// to get an instance of your DbContext to work with.
    /// </summary>
    /// <param name="args">Command line arguments passed to the EF tool</param>
    /// <returns>A new instance of ApplicationDbContext</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Load environment variables from .env file if it exists
        // Try multiple paths to handle running from project root or solution root
        var pathsToTry = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "attendance_monitoring", ".env")
        };

        foreach (var path in pathsToTry)
        {
            if (File.Exists(path))
            {
                try 
                { 
                    DotNetEnv.Env.Load(path); 
                    break;
                } 
                catch { /* Ignore load errors */ }
            }
        }

        // Set up configuration similar to the main Program.cs
        // IMPORTANT: AddEnvironmentVariables must be called AFTER AddJsonFile 
        // to ensure environment variables override values in appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Use in-memory database for design-time operations if no connection string is provided
            // This allows migrations to work without requiring a real database during development
            optionsBuilder.UseInMemoryDatabase("DesignTimeAttendanceDb");
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString, sqlOptions => 
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null));
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}