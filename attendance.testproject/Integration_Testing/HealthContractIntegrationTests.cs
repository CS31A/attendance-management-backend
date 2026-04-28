using System.Net;
using System.Text.Json;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace attendance.testproject.Integration_Testing;

public sealed class HealthContractIntegrationTests
{
    [Fact]
    public async Task GetHealthLive_ReturnsHealthy()
    {
        await using var host = await HealthEndpointHost.CreateAsync();

        var response = await host.Client.GetAsync("/health/live");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("status", out var status));
        Assert.Equal("Healthy", status.GetString());
        Assert.True(payload.RootElement.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task GetHealthReady_Returns200WithChecks()
    {
        await using var host = await HealthEndpointHost.CreateAsync();

        var response = await host.Client.GetAsync("/health/ready");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("status", out var status));
        Assert.True(payload.RootElement.TryGetProperty("timestamp", out _));
        Assert.True(payload.RootElement.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetHealthDetailed_ReturnsAllChecks()
    {
        await using var host = await HealthEndpointHost.CreateAsync();

        var response = await host.Client.GetAsync("/health");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("status", out var status));
        Assert.True(payload.RootElement.TryGetProperty("timestamp", out _));
        Assert.True(payload.RootElement.TryGetProperty("totalDuration", out _));
        Assert.True(payload.RootElement.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() >= 1);

        foreach (var check in checks.EnumerateArray())
        {
            Assert.True(check.TryGetProperty("name", out _));
            Assert.True(check.TryGetProperty("status", out _));
            Assert.True(check.TryGetProperty("duration", out _));
        }
    }

    [Fact]
    public async Task GetHealthReady_ReturnsUnhealthy_WhenDatabaseIsUnavailable()
    {
        // Use a connection string pointing to a nonexistent directory to force SQLite failure
        var connectionString = "Data Source=/invalid-nonexistent-path/db.sqlite";
        await using var host = await HealthEndpointHost.CreateAsync(connectionString);
        var response = await host.Client.GetAsync("/health/ready");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("status", out var status));
        Assert.Equal("Unhealthy", status.GetString());
    }

    private sealed class HealthEndpointHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SqliteConnection? _connection;

        private HealthEndpointHost(
            WebApplication app,
            HttpClient client,
            SqliteConnection? connection)
        {
            _app = app;
            Client = client;
            _connection = connection;
        }

        public HttpClient Client { get; }

        public static async Task<HealthEndpointHost> CreateAsync(string? connectionStringOverride = null)
        {
            var useRealDb = string.IsNullOrEmpty(connectionStringOverride);
            var connectionString = connectionStringOverride
                ?? $"Data Source=file:health-integration-{Guid.NewGuid():N}?mode=memory&cache=shared";

            SqliteConnection? connection = null;
            if (useRealDb)
            {
                connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
            }

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });

            builder.WebHost.UseTestServer();
            builder.Services.AddLogging();
            builder.Services.AddRouting();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

            builder.Services.AddHealthCheckServices();

            var cleanupService = new Mock<IOrphanedUserCleanupService>(MockBehavior.Strict);
            cleanupService
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

            builder.Services.AddSingleton(cleanupService);
            builder.Services.AddSingleton<IOrphanedUserCleanupService>(
                sp => sp.GetRequiredService<Mock<IOrphanedUserCleanupService>>().Object);

            var app = builder.Build();

            app.MapHealthCheckEndpoints();

            if (useRealDb)
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();
                }
            }

            await app.StartAsync();

            var client = app.GetTestClient();
            client.BaseAddress = new Uri("https://localhost");

            return new HealthEndpointHost(app, client, connection);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }
}
