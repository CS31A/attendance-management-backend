using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;

namespace attendance.testproject.Integration_Testing;

public sealed class GlobalExceptionHandlerIntegrationTests
{
    [Fact]
    public async Task UseGlobalExceptionHandler_ReturnsBadRequest_ForValidationException()
    {
        await using var testHost = await CreateTestHostAsync(
            () => throw new ValidationException("Validation failed for test request"));

        var response = await testHost.Client.GetAsync("/throw");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
        Assert.Equal("Validation failed for test request", error.Message);
        Assert.Equal("/throw", error.Path);
        Assert.Null(error.Details);
    }

    [Fact]
    public async Task UseGlobalExceptionHandler_ReturnsNotFound_ForEntityNotFoundException()
    {
        await using var testHost = await CreateTestHostAsync(
            () => throw new EntityNotFoundException<int>("Subject", 42));

        var response = await testHost.Client.GetAsync("/throw");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(StatusCodes.Status404NotFound, error.StatusCode);
        Assert.Equal("Subject with ID 42 was not found.", error.Message);
        Assert.Equal("/throw", error.Path);
    }

    [Fact]
    public async Task UseGlobalExceptionHandler_ReturnsInternalServerError_ForUnexpectedException()
    {
        await using var testHost = await CreateTestHostAsync(
            () => throw new InvalidOperationException("Unexpected failure"));

        var response = await testHost.Client.GetAsync("/throw");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
        Assert.Equal(
            "An unexpected error occurred. Please contact support if this persists.",
            error.Message);
        Assert.Equal("/throw", error.Path);
        Assert.Null(error.Details);
    }

    [Fact]
    public async Task UseGlobalExceptionHandler_ReturnsServiceUnavailable_ForDbUpdateException()
    {
        await using var testHost = await CreateTestHostAsync(
            () => new DbUpdateException("Database write failed"));

        var response = await testHost.Client.GetAsync("/throw");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error.StatusCode);
        Assert.Equal("Database is temporarily unavailable. Please try again later.", error.Message);
        Assert.Equal("/throw", error.Path);
        Assert.Null(error.Details);
    }

    private static async Task<TestHostHandle> CreateTestHostAsync(Func<Exception> exceptionFactory)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();

        var app = builder.Build();

        app.UseGlobalExceptionHandler();
        app.MapGet("/throw", (HttpContext _) => throw exceptionFactory());

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
