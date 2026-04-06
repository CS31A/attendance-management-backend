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
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Live()
    {
        return Task.FromResult<IActionResult>(Ok(new HealthStatusResponseDto
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Service = ServiceName
        }));
    }

    /// <summary>
    /// Reports readiness using database and data-integrity dependency status.
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Ready()
    {
        return BuildReadinessResponseAsync();
    }

    /// <summary>
    /// Compatibility alias for readiness semantics.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status503ServiceUnavailable)]
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
                    Status = "unhealthy",
                    Timestamp = timestamp,
                    Service = ServiceName,
                    Database = new HealthComponentStatusDto
                    {
                        Status = "unhealthy",
                        Connected = false,
                        Error = "Database connection failed"
                    },
                    DataIntegrity = new DataIntegrityStatusResponseDto
                    {
                        Status = "unhealthy",
                        Error = "Data integrity check skipped because the database connection failed."
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
                    Status = "healthy",
                    Connected = true
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
                Status = "unhealthy",
                Timestamp = timestamp,
                Service = ServiceName,
                Database = new HealthComponentStatusDto
                {
                    Status = "unhealthy",
                    Connected = false,
                    Error = ex.Message
                },
                DataIntegrity = new DataIntegrityStatusResponseDto
                {
                    Status = "unhealthy",
                    Error = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Data integrity health check for orphaned users and soft delete consistency.
    /// </summary>
    [HttpGet("data-integrity")]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status503ServiceUnavailable)]
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
                    Status = "unhealthy",
                    Error = ex.Message
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
            Status = overallStatus,
            Timestamp = timestamp,
            Service = ServiceName,
            Database = database,
            DataIntegrity = dataIntegrity
        };
    }

    private static DataIntegrityStatusResponseDto CreateDataIntegrityResponse(DataIntegrityHealthEvaluation evaluation)
    {
        return new DataIntegrityStatusResponseDto
        {
            Status = evaluation.Status.ToString().ToLowerInvariant(),
            OrphanedUserCount = evaluation.IntegrityStatus.OrphanedUserCount,
            TotalSoftDeleteInconsistencies = evaluation.TotalSoftDeleteIssues,
            SoftDeleteInconsistencies = new SoftDeleteInconsistenciesResponseDto
            {
                Students = evaluation.IntegrityStatus.StudentsWithInconsistentSoftDelete,
                Instructors = evaluation.IntegrityStatus.InstructorsWithInconsistentSoftDelete,
                Admins = evaluation.IntegrityStatus.AdminsWithInconsistentSoftDelete
            },
            CheckedAt = evaluation.IntegrityStatus.CheckedAt,
            IsHealthy = evaluation.IntegrityStatus.IsHealthy
        };
    }
}
