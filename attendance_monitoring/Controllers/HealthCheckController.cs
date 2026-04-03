using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Health")]
public class HealthCheckController(
    ApplicationDbContext applicationDbContext,
    IOrphanedUserCleanupService orphanedUserCleanupService,
    ILogger<HealthCheckController> logger) : ControllerBase
{
    private const string ServiceName = "Attendance Monitoring API";

    /// <summary>
    /// Reports process liveness without checking external dependencies.
    /// </summary>
    [HttpGet("live")]
    public Task<IActionResult> Live()
    {
        return Task.FromResult<IActionResult>(Ok(new HealthStatusResponseDto
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = ServiceName
        }));
    }

    /// <summary>
    /// Reports readiness using database and data-integrity dependency status.
    /// </summary>
    [HttpGet("ready")]
    public Task<IActionResult> Ready()
    {
        return BuildReadinessResponseAsync();
    }

    /// <summary>
    /// Compatibility alias for readiness semantics.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> HealthCheck()
    {
        return BuildReadinessResponseAsync();
    }

    private async Task<IActionResult> BuildReadinessResponseAsync()
    {
        var timestamp = DateTime.UtcNow;

        try
        {
            var canConnect = await applicationDbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                return StatusCode(503, new HealthStatusResponseDto
                {
                    status = "unhealthy",
                    timestamp = timestamp,
                    service = ServiceName,
                    database = new HealthComponentStatusDto
                    {
                        status = "unhealthy",
                        connected = false,
                        error = "Database connection failed"
                    },
                    dataIntegrity = new DataIntegrityStatusResponseDto
                    {
                        status = "unhealthy",
                        error = "Data integrity check skipped because the database connection failed."
                    }
                });
            }

            var integrityStatus = await orphanedUserCleanupService.GetDataIntegrityStatusAsync();
            var integrityEvaluation = DataIntegrityHealthCheck.Evaluate(integrityStatus);
            var response = CreateHealthResponse(
                integrityEvaluation.Status == HealthStatus.Healthy ? "healthy" : integrityEvaluation.Status.ToString().ToLowerInvariant(),
                timestamp,
                new HealthComponentStatusDto
                {
                    status = "healthy",
                    connected = true
                },
                CreateDataIntegrityResponse(integrityEvaluation));

            return integrityEvaluation.Status == HealthStatus.Unhealthy
                ? StatusCode(503, response)
                : Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server health error");
            return StatusCode(503, new HealthStatusResponseDto
            {
                status = "unhealthy",
                timestamp = timestamp,
                service = ServiceName,
                database = new HealthComponentStatusDto
                {
                    status = "unhealthy",
                    connected = false,
                    error = ex.Message
                },
                dataIntegrity = new DataIntegrityStatusResponseDto
                {
                    status = "unhealthy",
                    error = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Data integrity health check for orphaned users and soft delete consistency.
    /// </summary>
    [HttpGet("data-integrity")]
    public async Task<IActionResult> DataIntegrityCheck()
    {
        try
        {
            var status = await orphanedUserCleanupService.GetDataIntegrityStatusAsync();
            var evaluation = DataIntegrityHealthCheck.Evaluate(status);
            var response = CreateHealthResponse(
                evaluation.Status.ToString().ToLowerInvariant(),
                DateTime.UtcNow,
                database: null,
                dataIntegrity: CreateDataIntegrityResponse(evaluation));

            return StatusCode(evaluation.Status == HealthStatus.Unhealthy ? 503 : 200, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data integrity health check error");
            return StatusCode(503, CreateHealthResponse(
                "unhealthy",
                DateTime.UtcNow,
                database: null,
                dataIntegrity: new DataIntegrityStatusResponseDto
                {
                    status = "unhealthy",
                    error = ex.Message
                }));
        }
    }

    private static HealthStatusResponseDto CreateHealthResponse(
        string overallStatus,
        DateTime timestamp,
        HealthComponentStatusDto? database,
        DataIntegrityStatusResponseDto? dataIntegrity)
    {
        return new HealthStatusResponseDto
        {
            status = overallStatus,
            timestamp = timestamp,
            service = ServiceName,
            database = database,
            dataIntegrity = dataIntegrity
        };
    }

    private static DataIntegrityStatusResponseDto CreateDataIntegrityResponse(DataIntegrityHealthEvaluation evaluation)
    {
        return new DataIntegrityStatusResponseDto
        {
            status = evaluation.Status.ToString().ToLowerInvariant(),
            orphanedUserCount = evaluation.IntegrityStatus.OrphanedUserCount,
            totalSoftDeleteInconsistencies = evaluation.TotalSoftDeleteIssues,
            softDeleteInconsistencies = new SoftDeleteInconsistenciesResponseDto
            {
                students = evaluation.IntegrityStatus.StudentsWithInconsistentSoftDelete,
                instructors = evaluation.IntegrityStatus.InstructorsWithInconsistentSoftDelete,
                admins = evaluation.IntegrityStatus.AdminsWithInconsistentSoftDelete
            },
            checkedAt = evaluation.IntegrityStatus.CheckedAt,
            isHealthy = evaluation.IntegrityStatus.IsHealthy
        };
    }
}
