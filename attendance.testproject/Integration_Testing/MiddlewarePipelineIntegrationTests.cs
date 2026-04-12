using attendance_monitoring.Extensions.WebApplicationExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace attendance.testproject.Integration_Testing;

public sealed class MiddlewarePipelineIntegrationTests
{
    [Fact]
    public async Task UseCorePipeline_AppliesHttpsRedirection_InDevelopmentEnvironment()
    {
        await using var host = await CreateTestHostAsync(Environments.Development);

        var response = await host.Client.GetAsync("http://localhost/test");

        Assert.Equal(System.Net.HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https://localhost:5001/test", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task UseCorePipeline_SkipsHttpsRedirection_InProductionEnvironment()
    {
        await using var host = await CreateTestHostAsync(Environments.Production);

        var response = await host.Client.GetAsync("http://localhost/test");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void IsDevelopment_ReturnsTrueForDevelopmentEnvironment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        Assert.True(app.Environment.IsDevelopment());
    }

    [Fact]
    public void IsDevelopment_ReturnsFalseForProductionEnvironment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        Assert.False(app.Environment.IsDevelopment());
    }

    private static async Task<TestHostHandle> CreateTestHostAsync(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });

        builder.WebHost.UseTestServer();

        // Register all services required by UseCorePipeline:
        // UseCors needs a named policy, UseAuthentication/UseAuthorization
        // need auth services, MapControllers needs MVC, MapSignalRHubs needs SignalR.
        builder.Services.AddLogging();
        builder.Services.AddHttpsRedirection(options => options.HttpsPort = 5001);
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();

        // Call the real extension method under test
        app.UseCorePipeline();
        app.MapGet("/test", () => "ok");

        await app.StartAsync();

        return new TestHostHandle(app, app.GetTestClient());
    }

    private sealed class TestHostHandle(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
