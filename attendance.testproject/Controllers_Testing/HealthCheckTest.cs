using attendance_monitoring.Controllers;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;


namespace attendance.testproject.Controllers_Testing;

public class HealthCheckControllerTest
{
    private readonly Mock<ApplicationDbContext> _mockDbContext;
    private readonly Mock<DbFacade> _mockDbFacade;
    private readonly Mock<IOrphanedUserCleanupService> _mockOrphanedUserCleanupService;
    private readonly Mock<ILogger<HealthCheckController>> _mockLogger;
    private readonly HealthCheckController _controller;

    public abstract class DbFacade : DatabaseFacade
    {
        protected DbFacade(DbContext context) : base(context) { }

        public abstract override Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    }

    public HealthCheckControllerTest()
    {
        _mockLogger = new Mock<ILogger<HealthCheckController>>();
        _mockOrphanedUserCleanupService = new Mock<IOrphanedUserCleanupService>();

        _mockOrphanedUserCleanupService
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

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockDbContext = new Mock<ApplicationDbContext>(options);

        _mockDbFacade = new Mock<DbFacade>(_mockDbContext.Object);

        _mockDbContext.Setup(c => c.Database).Returns(_mockDbFacade.Object);

        _controller = new HealthCheckController(
            _mockDbContext.Object,
            _mockOrphanedUserCleanupService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void HealthController_DefinesExplicitLiveReadyAndCompatibilityRoutes()
    {
        var methods = typeof(HealthCheckController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(HealthCheckController))
            .ToList();

        var routeTemplates = methods
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                .Cast<HttpMethodAttribute>()
                .Select(attribute => attribute.Template ?? string.Empty))
            .ToList();

        Assert.Contains("live", routeTemplates);
        Assert.Contains("ready", routeTemplates);
        Assert.Contains(string.Empty, routeTemplates);
    }

    [Fact]
    public async Task HealthCheckAlias_UsesReadinessSemantics_WhenDatabaseIsUnavailable()
    {
        _mockDbFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var readyResult = await InvokeHealthActionByRouteAsync("ready");
        var aliasResult = await InvokeHealthActionByRouteAsync(string.Empty);

        var readyStatusCode = Assert.IsType<ObjectResult>(readyResult);
        var aliasStatusCode = Assert.IsType<ObjectResult>(aliasResult);

        Assert.Equal(503, readyStatusCode.StatusCode);
        Assert.Equal(readyStatusCode.StatusCode, aliasStatusCode.StatusCode);

        var readyStatus = readyStatusCode.Value?.GetType().GetProperty("status")?.GetValue(readyStatusCode.Value);
        var aliasStatus = aliasStatusCode.Value?.GetType().GetProperty("status")?.GetValue(aliasStatusCode.Value);

        Assert.Equal(readyStatus, aliasStatus);
    }

    [Fact]
    public async Task LiveEndpoint_ReturnsHealthyWithoutDatabaseDependency()
    {
        _mockDbFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var liveResult = await InvokeHealthActionByRouteAsync("live");

        var okResult = Assert.IsType<OkObjectResult>(liveResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal("healthy", okResult.Value?.GetType().GetProperty("status")?.GetValue(okResult.Value));
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk_WhenDatabaseIsHealthy()
    {
        _mockDbFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.HealthCheck();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var responseValue = okResult.Value;
        Assert.NotNull(responseValue);

        // Use reflection or JSON serialization to access anonymous object properties
        var statusProperty = responseValue.GetType().GetProperty("status");
        var databaseProperty = responseValue.GetType().GetProperty("database");

        Assert.NotNull(statusProperty);
        Assert.NotNull(databaseProperty);

        Assert.Equal("healthy", statusProperty.GetValue(responseValue));

        var databaseValue = databaseProperty.GetValue(responseValue);
        Assert.NotNull(databaseValue);

        var dbStatusProperty = databaseValue.GetType().GetProperty("status");
        var dbConnectedProperty = databaseValue.GetType().GetProperty("connected");

        Assert.NotNull(dbStatusProperty);
        Assert.NotNull(dbConnectedProperty);

        Assert.Equal("healthy", dbStatusProperty.GetValue(databaseValue));
        Assert.Equal(true, dbConnectedProperty.GetValue(databaseValue));

        _mockDbFacade.Verify(db => db.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealthCheck_ReturnsServiceUnavailable_WhenDatabaseIsUnhealthy()
    {
        _mockDbFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.HealthCheck();

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);

        var responseValue = statusCodeResult.Value;
        Assert.NotNull(responseValue);

        var statusProperty = responseValue.GetType().GetProperty("status");
        var databaseProperty = responseValue.GetType().GetProperty("database");

        Assert.NotNull(statusProperty);
        Assert.NotNull(databaseProperty);

        Assert.Equal("unhealthy", statusProperty.GetValue(responseValue));

        var databaseValue = databaseProperty.GetValue(responseValue);
        Assert.NotNull(databaseValue);

        var dbStatusProperty = databaseValue.GetType().GetProperty("status");
        var dbConnectedProperty = databaseValue.GetType().GetProperty("connected");
        var dbErrorProperty = databaseValue.GetType().GetProperty("error");

        Assert.NotNull(dbStatusProperty);
        Assert.NotNull(dbConnectedProperty);
        Assert.NotNull(dbErrorProperty);

        Assert.Equal("unhealthy", dbStatusProperty.GetValue(databaseValue));
        Assert.Equal(false, dbConnectedProperty.GetValue(databaseValue));
        Assert.Equal("Database connection failed", dbErrorProperty.GetValue(databaseValue));

        _mockDbFacade.Verify(db => db.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealthCheck_ReturnsServiceUnavailable_WhenAnExceptionIsThrown()
    {
        var exceptionMessage = "Simulated network failure.";
        var expectedException = new Exception(exceptionMessage);

        _mockDbFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var result = await _controller.HealthCheck();

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);

        var responseValue = statusCodeResult.Value;
        Assert.NotNull(responseValue);

        var statusProperty = responseValue.GetType().GetProperty("status");
        var databaseProperty = responseValue.GetType().GetProperty("database");

        Assert.NotNull(statusProperty);
        Assert.NotNull(databaseProperty);

        Assert.Equal("unhealthy", statusProperty.GetValue(responseValue));

        var databaseValue = databaseProperty.GetValue(responseValue);
        Assert.NotNull(databaseValue);

        var dbStatusProperty = databaseValue.GetType().GetProperty("status");
        var dbConnectedProperty = databaseValue.GetType().GetProperty("connected");
        var dbErrorProperty = databaseValue.GetType().GetProperty("error");

        Assert.NotNull(dbStatusProperty);
        Assert.NotNull(dbConnectedProperty);
        Assert.NotNull(dbErrorProperty);

        Assert.Equal("unhealthy", dbStatusProperty.GetValue(databaseValue));
        Assert.Equal(false, dbConnectedProperty.GetValue(databaseValue));
        Assert.Equal(exceptionMessage, dbErrorProperty.GetValue(databaseValue));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once
        );
    }

    private async Task<IActionResult> InvokeHealthActionByRouteAsync(string routeTemplate)
    {
        var method = typeof(HealthCheckController)
            .GetMethods()
            .SingleOrDefault(candidate => candidate.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                .Cast<HttpMethodAttribute>()
                .Any(attribute => (attribute.Template ?? string.Empty) == routeTemplate));

        Assert.NotNull(method);

        var result = method!.Invoke(_controller, null);
        var task = Assert.IsAssignableFrom<Task<IActionResult>>(result);
        return await task;
    }
}
