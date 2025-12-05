using attendance_monitoring.Data;
using attendance_monitoring.IServices;
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
    /// <summary>
    /// Basic health check for the API and database connectivity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Check database connectivity
            var canConnect = await applicationDbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "Attendance Monitoring API",
                    database = new { status = "unhealthy", connected = false, error = "Database connection failed" }
                });
            }

            return Ok(
                new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "Attendance Monitoring API",
                    database = new { status = "healthy", connected = true }
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server health error");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "Attendance Monitoring API",
                database = new { status = "unhealthy", connected = false, error = ex.Message }
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

            var hasSoftDeleteIssues = status.StudentsWithInconsistentSoftDelete > 0 ||
                                      status.InstructorsWithInconsistentSoftDelete > 0 ||
                                      status.AdminsWithInconsistentSoftDelete > 0;

            var healthStatus = status.OrphanedUserCount switch
            {
                0 when !hasSoftDeleteIssues => "healthy",
                < 5 => "degraded",
                _ => "unhealthy"
            };

            var statusCode = healthStatus switch
            {
                "healthy" => 200,
                "degraded" => 200,
                _ => 503
            };

            return StatusCode(statusCode, new
            {
                status = healthStatus,
                timestamp = DateTime.UtcNow,
                service = "Attendance Monitoring API",
                dataIntegrity = new
                {
                    orphanedUserCount = status.OrphanedUserCount,
                    softDeleteInconsistencies = new
                    {
                        students = status.StudentsWithInconsistentSoftDelete,
                        instructors = status.InstructorsWithInconsistentSoftDelete,
                        admins = status.AdminsWithInconsistentSoftDelete
                    },
                    checkedAt = status.CheckedAt,
                    isHealthy = status.IsHealthy
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data integrity health check error");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "Attendance Monitoring API",
                dataIntegrity = new { error = ex.Message }
            });
        }
    }
}