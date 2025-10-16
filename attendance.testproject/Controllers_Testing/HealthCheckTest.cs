using attendance_monitoring.Controllers;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;


namespace attendance.testproject.Controllers_Testing;

public class HealthCheckControllerTest
{
    private readonly Mock<ApplicationDbContext> _mockDbContext;
    private readonly Mock<DbFacade> _mockDbFacade;
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

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockDbContext = new Mock<ApplicationDbContext>(options);

        _mockDbFacade = new Mock<DbFacade>(_mockDbContext.Object);

        _mockDbContext.Setup(c => c.Database).Returns(_mockDbFacade.Object);

        _controller = new HealthCheckController(_mockDbContext.Object, _mockLogger.Object);
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

        dynamic responseValue = okResult.Value;
        Assert.Equal("healthy", responseValue.status);
        Assert.Equal("healthy", responseValue.database.status);
        Assert.True(responseValue.database.connected);

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

        dynamic responseValue = statusCodeResult.Value;
        Assert.Equal("unhealthy", responseValue.status);
        Assert.Equal("unhealthy", responseValue.database.status);
        Assert.False(responseValue.database.connected);
        Assert.Equal("Database connection failed", responseValue.database.error);

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

        dynamic responseValue = statusCodeResult.Value;
        Assert.Equal("unhealthy", responseValue.status);
        Assert.Equal("unhealthy", responseValue.database.status);
        Assert.False(responseValue.database.connected);
        Assert.Equal(exceptionMessage, responseValue.database.error);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once
        );
    }
}