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
    /// Threshold for unhealthy status (number of orphaned users).
    /// </summary>
    internal const int UnhealthyThreshold = 20;

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
            var evaluation = Evaluate(status);

            return evaluation.Status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(evaluation.Description, evaluation.ToDataDictionary()),
                HealthStatus.Degraded => HealthCheckResult.Degraded(evaluation.Description, data: evaluation.ToDataDictionary()),
                _ => HealthCheckResult.Unhealthy(evaluation.Description, data: evaluation.ToDataDictionary())
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check data integrity.",
                ex);
        }
    }

    /// <summary>
    /// Evaluates a data-integrity snapshot against the Phase 3 health contract.
    /// </summary>
    internal static DataIntegrityHealthEvaluation Evaluate(DataIntegrityStatus status)
    {
        var totalSoftDeleteIssues = status.StudentsWithInconsistentSoftDelete +
                                    status.InstructorsWithInconsistentSoftDelete +
                                    status.AdminsWithInconsistentSoftDelete;

        if (status.OrphanedUserCount == 0 && totalSoftDeleteIssues == 0)
        {
            return new DataIntegrityHealthEvaluation(
                HealthStatus.Healthy,
                "Data integrity is healthy. No orphaned users or soft delete inconsistencies detected.",
                status);
        }

        if (totalSoftDeleteIssues > 0 || status.OrphanedUserCount >= UnhealthyThreshold)
        {
            return new DataIntegrityHealthEvaluation(
                HealthStatus.Unhealthy,
                $"Data integrity issues detected: {status.OrphanedUserCount} orphaned users, {totalSoftDeleteIssues} soft delete inconsistencies.",
                status,
                totalSoftDeleteIssues);
        }

        return new DataIntegrityHealthEvaluation(
            HealthStatus.Degraded,
            $"Data integrity drift detected: {status.OrphanedUserCount} orphaned users.",
            status,
            totalSoftDeleteIssues);
    }
}

/// <summary>
/// Shared evaluation details for controller and health-check responses.
/// </summary>
internal sealed class DataIntegrityHealthEvaluation
{
    public DataIntegrityHealthEvaluation(
        HealthStatus status,
        string description,
        DataIntegrityStatus integrityStatus,
        int? totalSoftDeleteIssues = null)
    {
        Status = status;
        Description = description;
        IntegrityStatus = integrityStatus;
        TotalSoftDeleteIssues = totalSoftDeleteIssues ?? integrityStatus.StudentsWithInconsistentSoftDelete +
            integrityStatus.InstructorsWithInconsistentSoftDelete +
            integrityStatus.AdminsWithInconsistentSoftDelete;
    }

    public HealthStatus Status { get; }

    public string Description { get; }

    public DataIntegrityStatus IntegrityStatus { get; }

    public int TotalSoftDeleteIssues { get; }

    public Dictionary<string, object> ToDataDictionary()
    {
        return new Dictionary<string, object>
        {
            ["orphanedUserCount"] = IntegrityStatus.OrphanedUserCount,
            ["studentsWithInconsistentSoftDelete"] = IntegrityStatus.StudentsWithInconsistentSoftDelete,
            ["instructorsWithInconsistentSoftDelete"] = IntegrityStatus.InstructorsWithInconsistentSoftDelete,
            ["adminsWithInconsistentSoftDelete"] = IntegrityStatus.AdminsWithInconsistentSoftDelete,
            ["totalSoftDeleteInconsistencies"] = TotalSoftDeleteIssues,
            ["checkedAt"] = IntegrityStatus.CheckedAt.ToString("O")
        };
    }
}
