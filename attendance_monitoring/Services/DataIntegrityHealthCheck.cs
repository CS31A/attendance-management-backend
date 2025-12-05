using attendance_monitoring.IServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace attendance_monitoring.Services;

/// <summary>
/// Health check for monitoring data integrity in the attendance management system.
/// 
/// This health check verifies:
/// 1. No orphaned users exist (users without corresponding profiles)
/// 2. Soft delete consistency (IsDeleted flag matches DeletedAt timestamp)
/// 
/// Healthy: No data integrity issues detected
/// Degraded: Minor issues detected (e.g., few orphaned users)
/// Unhealthy: Significant data integrity issues
/// </summary>
public class DataIntegrityHealthCheck(IOrphanedUserCleanupService cleanupService) : IHealthCheck
{
    /// <summary>
    /// Threshold for degraded status (number of orphaned users).
    /// </summary>
    private const int DegradedThreshold = 5;

    /// <summary>
    /// Threshold for unhealthy status (number of orphaned users).
    /// </summary>
    private const int UnhealthyThreshold = 20;

    /// <summary>
    /// Performs the health check.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await cleanupService.GetDataIntegrityStatusAsync();

            var data = new Dictionary<string, object>
            {
                ["orphanedUserCount"] = status.OrphanedUserCount,
                ["studentsWithInconsistentSoftDelete"] = status.StudentsWithInconsistentSoftDelete,
                ["instructorsWithInconsistentSoftDelete"] = status.InstructorsWithInconsistentSoftDelete,
                ["adminsWithInconsistentSoftDelete"] = status.AdminsWithInconsistentSoftDelete,
                ["checkedAt"] = status.CheckedAt.ToString("O")
            };

            // Calculate total issues
            var totalSoftDeleteIssues = status.StudentsWithInconsistentSoftDelete +
                                        status.InstructorsWithInconsistentSoftDelete +
                                        status.AdminsWithInconsistentSoftDelete;

            var totalIssues = status.OrphanedUserCount + totalSoftDeleteIssues;

            // Determine health status
            if (totalIssues == 0)
            {
                return HealthCheckResult.Healthy(
                    "Data integrity is healthy. No orphaned users or soft delete inconsistencies detected.",
                    data);
            }

            if (status.OrphanedUserCount >= UnhealthyThreshold || totalSoftDeleteIssues > 0)
            {
                return HealthCheckResult.Unhealthy(
                    $"Data integrity issues detected: {status.OrphanedUserCount} orphaned users, " +
                    $"{totalSoftDeleteIssues} soft delete inconsistencies.",
                    data: data);
            }

            if (status.OrphanedUserCount >= DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Minor data integrity issues detected: {status.OrphanedUserCount} orphaned users.",
                    data: data);
            }

            // Few issues, but still report as degraded
            return HealthCheckResult.Degraded(
                $"Data integrity has minor issues: {totalIssues} total issues detected.",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check data integrity.",
                ex);
        }
    }
}
