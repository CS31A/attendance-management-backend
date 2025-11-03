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
        // Set up configuration similar to the main Program.cs
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .Build();

        // Load environment variables from .env file if it exists
        try
        {
            DotNetEnv.Env.Load();
        }
        catch
        {
            // If .env file doesn't exist, continue with just appsettings
        }

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
            optionsBuilder.UseSqlServer(connectionString);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}