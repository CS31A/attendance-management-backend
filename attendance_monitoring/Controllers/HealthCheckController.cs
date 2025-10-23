using attendance_monitoring.Data;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Health")]
public class HealthCheckController(ApplicationDbContext applicationDbContext, ILogger<HealthCheckController> logger): ControllerBase
{
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
}