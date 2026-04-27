using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using attendance_monitoring.Extensions;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace attendance.testproject.Integration_Testing;

public sealed class NotificationHubAuthIntegrationTests
{
    private const string JwtSecret = "test-secret-key-for-integration-testing-minimum-32-characters";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";

    [Fact]
    public async Task PostNotificationHubNegotiate_RejectsUnauthenticatedRequests()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.PostAsync("/notificationHub/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostNotificationHubNegotiate_AcceptsValidAccessTokenCookie()
    {
        await using var host = await CreateHostAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/notificationHub/negotiate?negotiateVersion=1");
        request.Headers.Add("Cookie", $"accessToken={CreateJwt()}");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostNotificationHubNegotiate_AcceptsValidAccessTokenQueryString()
    {
        await using var host = await CreateHostAsync();

        var response = await host.Client.PostAsync($"/notificationHub/negotiate?negotiateVersion=1&access_token={CreateJwt()}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<SignalRAuthHost> CreateHostAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:Token"] = JwtSecret,
            ["AppSettings:Issuer"] = JwtIssuer,
            ["AppSettings:Audience"] = JwtAudience,
            ["TimeZoneSettings:TimeZoneId"] = TimeZoneInfo.Local.Id,
            ["SessionAutoEnd:Enabled"] = "false"
        });

        var tokenValidationService = new Mock<ITokenValidationService>(MockBehavior.Strict);
        tokenValidationService
            .Setup(service => service.IsTokenBlacklistedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        builder.Services.AddSingleton(tokenValidationService.Object);
        builder.Services.AddAuthenticationServices(builder.Configuration);
        builder.Services.AddAuthorizationPolicies();
        builder.Services.AddSignalRServices();

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSignalRHubs();

        await app.StartAsync();
        var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        return new SignalRAuthHost(app, client);
    }

    private static string CreateJwt()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class SignalRAuthHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}
