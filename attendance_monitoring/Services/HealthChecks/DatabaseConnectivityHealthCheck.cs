using attendance_monitoring.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace attendance_monitoring.Services.HealthChecks;

/// <summary>
/// Health check for verifying database connectivity for readiness evaluation.
/// </summary>
internal sealed class DatabaseConnectivityHealthCheck(ApplicationDbContext applicationDbContext) : IHealthCheck
{
    /// <summary>
    /// Verifies that the application database is reachable.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await applicationDbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database connectivity is healthy.", new Dictionary<string, object> { ["connected"] = true })
                : HealthCheckResult.Unhealthy(
                    "Database connection failed.",
                    data: new Dictionary<string, object>
                    {
                        ["connected"] = false,
                        ["error"] = "Database connection failed"
                    });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed.",
                ex,
                new Dictionary<string, object>
                {
                    ["connected"] = false,
                    ["error"] = ex.Message
                });
        }
    }
}
