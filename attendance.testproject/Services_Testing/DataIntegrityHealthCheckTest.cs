using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace attendance.testproject.Services_Testing;

public class DataIntegrityHealthCheckTest
{
    private readonly Mock<IOrphanedUserCleanupService> _cleanupService = new();
    private readonly HealthCheckContext _healthCheckContext = new();

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenNoIntegrityIssuesExist()
    {
        _cleanupService
            .Setup(service => service.GetDataIntegrityStatusAsync())
            .ReturnsAsync(new DataIntegrityStatus
            {
                IsHealthy = true,
                OrphanedUserCount = 0,
                StudentsWithInconsistentSoftDelete = 0,
                InstructorsWithInconsistentSoftDelete = 0,
                AdminsWithInconsistentSoftDelete = 0,
                CheckedAt = DateTime.UtcNow
            });

        var result = await CreateHealthCheck().CheckHealthAsync(_healthCheckContext);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(0, Assert.IsType<int>(result.Data["orphanedUserCount"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["studentsWithInconsistentSoftDelete"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["instructorsWithInconsistentSoftDelete"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["adminsWithInconsistentSoftDelete"]));
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenOnlyLowLevelOrphanDriftExists()
    {
        _cleanupService
            .Setup(service => service.GetDataIntegrityStatusAsync())
            .ReturnsAsync(new DataIntegrityStatus
            {
                IsHealthy = false,
                OrphanedUserCount = 1,
                StudentsWithInconsistentSoftDelete = 0,
                InstructorsWithInconsistentSoftDelete = 0,
                AdminsWithInconsistentSoftDelete = 0,
                CheckedAt = DateTime.UtcNow
            });

        var result = await CreateHealthCheck().CheckHealthAsync(_healthCheckContext);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(1, Assert.IsType<int>(result.Data["orphanedUserCount"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["studentsWithInconsistentSoftDelete"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["instructorsWithInconsistentSoftDelete"]));
        Assert.Equal(0, Assert.IsType<int>(result.Data["adminsWithInconsistentSoftDelete"]));
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(20, 0, 0, 0)]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenSoftDeleteOrCriticalOrphanThresholdIsMet(
        int orphanedUserCount,
        int inconsistentStudents,
        int inconsistentInstructors,
        int inconsistentAdmins)
    {
        _cleanupService
            .Setup(service => service.GetDataIntegrityStatusAsync())
            .ReturnsAsync(new DataIntegrityStatus
            {
                IsHealthy = false,
                OrphanedUserCount = orphanedUserCount,
                StudentsWithInconsistentSoftDelete = inconsistentStudents,
                InstructorsWithInconsistentSoftDelete = inconsistentInstructors,
                AdminsWithInconsistentSoftDelete = inconsistentAdmins,
                CheckedAt = DateTime.UtcNow
            });

        var result = await CreateHealthCheck().CheckHealthAsync(_healthCheckContext);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(orphanedUserCount, Assert.IsType<int>(result.Data["orphanedUserCount"]));
        Assert.Equal(inconsistentStudents, Assert.IsType<int>(result.Data["studentsWithInconsistentSoftDelete"]));
        Assert.Equal(inconsistentInstructors, Assert.IsType<int>(result.Data["instructorsWithInconsistentSoftDelete"]));
        Assert.Equal(inconsistentAdmins, Assert.IsType<int>(result.Data["adminsWithInconsistentSoftDelete"]));
    }

    private DataIntegrityHealthCheck CreateHealthCheck() => new(_cleanupService.Object);
}
