using System.Net;
using System.Text.Json;
using attendance_monitoring.Controllers;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace attendance.testproject.Integration_Testing;

public sealed class HealthContractIntegrationTests
{
    [Fact]
    public async Task GetApiHealthReady_ReturnsCamelCaseHealthPayload()
    {
        await using var host = await HealthContractHost.CreateAsync();

        var response = await host.Client.GetAsync("/api/health/ready");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("status", out var status));
        Assert.True(payload.RootElement.TryGetProperty("timestamp", out _));
        Assert.True(payload.RootElement.TryGetProperty("service", out _));
        Assert.True(payload.RootElement.TryGetProperty("database", out var database));
        Assert.True(payload.RootElement.TryGetProperty("dataIntegrity", out var dataIntegrity));

        Assert.Equal("healthy", status.GetString());
        Assert.True(database.TryGetProperty("status", out var databaseStatus));
        Assert.True(database.TryGetProperty("connected", out var databaseConnected));
        Assert.Equal("healthy", databaseStatus.GetString());
        Assert.True(databaseConnected.GetBoolean());

        Assert.True(dataIntegrity.TryGetProperty("status", out var integrityStatus));
        Assert.True(dataIntegrity.TryGetProperty("orphanedUserCount", out _));
        Assert.True(dataIntegrity.TryGetProperty("totalSoftDeleteInconsistencies", out _));
        Assert.True(dataIntegrity.TryGetProperty("softDeleteInconsistencies", out var softDeleteInconsistencies));
        Assert.True(dataIntegrity.TryGetProperty("checkedAt", out _));
        Assert.True(dataIntegrity.TryGetProperty("isHealthy", out var integrityHealthy));
        Assert.Equal("healthy", integrityStatus.GetString());
        Assert.True(integrityHealthy.GetBoolean());

        Assert.True(softDeleteInconsistencies.TryGetProperty("students", out _));
        Assert.True(softDeleteInconsistencies.TryGetProperty("instructors", out _));
        Assert.True(softDeleteInconsistencies.TryGetProperty("admins", out _));

        Assert.False(payload.RootElement.TryGetProperty("Status", out _));
        Assert.False(database.TryGetProperty("Connected", out _));
        Assert.False(dataIntegrity.TryGetProperty("SoftDeleteInconsistencies", out _));
    }

    [Fact]
    public async Task GetSwaggerV1SwaggerJson_UsesCamelCaseHealthSchemaProperties()
    {
        await using var host = await HealthContractHost.CreateAsync();

        var response = await host.Client.GetAsync("/swagger/v1/swagger.json");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(schemas.TryGetProperty(nameof(HealthStatusResponseDto), out var healthSchema));
        Assert.True(schemas.TryGetProperty(nameof(HealthComponentStatusDto), out var componentSchema));
        Assert.True(schemas.TryGetProperty(nameof(DataIntegrityStatusResponseDto), out var dataIntegritySchema));
        Assert.True(schemas.TryGetProperty(nameof(SoftDeleteInconsistenciesResponseDto), out var softDeleteSchema));

        AssertContainsCamelCaseProperties(healthSchema, "status", "timestamp", "service", "database", "dataIntegrity");
        AssertContainsCamelCaseProperties(componentSchema, "status", "connected", "error");
        AssertContainsCamelCaseProperties(
            dataIntegritySchema,
            "orphanedUserCount",
            "totalSoftDeleteInconsistencies",
            "softDeleteInconsistencies",
            "checkedAt",
            "isHealthy",
            "status",
            "error");
        AssertContainsCamelCaseProperties(softDeleteSchema, "students", "instructors", "admins");
    }

    private static void AssertContainsCamelCaseProperties(JsonElement schema, params string[] propertyNames)
    {
        var properties = schema.GetProperty("properties");

        foreach (var propertyName in propertyNames)
        {
            Assert.True(properties.TryGetProperty(propertyName, out _), $"Expected schema property '{propertyName}'.");
            Assert.False(properties.TryGetProperty(ToPascalCase(propertyName), out _), $"Unexpected PascalCase schema property '{ToPascalCase(propertyName)}'.");
        }
    }

    private static string ToPascalCase(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    private sealed class HealthContractHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SqliteConnection _connection;

        private HealthContractHost(
            WebApplication app,
            HttpClient client,
            SqliteConnection connection)
        {
            _app = app;
            Client = client;
            _connection = connection;
        }

        public HttpClient Client { get; }

        public static async Task<HealthContractHost> CreateAsync()
        {
            var connectionString = $"Data Source=file:health-contract-{Guid.NewGuid():N}?mode=memory&cache=shared";
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });

            builder.WebHost.UseTestServer();
            builder.Services.AddLogging();
            builder.Services.AddRouting();
            builder.Services.AddResponseHandling();
            builder.Services.AddApiDocumentation();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
            builder.Services.AddControllers()
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Clear();
                    manager.ApplicationParts.Add(new AssemblyPart(typeof(HealthCheckController).Assembly));
                });

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
            builder.Services.AddSingleton<IOrphanedUserCleanupService>(sp => sp.GetRequiredService<Mock<IOrphanedUserCleanupService>>().Object);

            var app = builder.Build();
            app.UseDevelopmentTools();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            await app.StartAsync();

            var client = app.GetTestClient();
            client.BaseAddress = new Uri("https://localhost");

            return new HealthContractHost(app, client, connection);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
