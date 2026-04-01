using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using attendance_monitoring.Controllers;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Integration_Testing.Support;

internal sealed class ApiIntegrationHost : IAsyncDisposable
{
    internal const string AuthenticationScheme = "IntegrationTestBearer";

    private readonly WebApplication _app;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    private ApiIntegrationHost(WebApplication app, HttpClient client, Mock<IAccountService> accountService)
    {
        _app = app;
        Client = client;
        AccountService = accountService;
    }

    public HttpClient Client { get; }

    public Mock<IAccountService> AccountService { get; }

    public static async Task<ApiIntegrationHost> CreateAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Services
            .AddAuthentication(AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new AssemblyPart(typeof(AccountController).Assembly));
            });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CookieSettings:AccessTokenExpirationMinutes"] = "15",
            ["CookieSettings:RefreshTokenExpirationDays"] = "7"
        });

        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        builder.Services.AddSingleton(accountService);
        builder.Services.AddSingleton<IAccountService>(sp => sp.GetRequiredService<Mock<IAccountService>>().Object);
        builder.Services.AddScoped<ICookieOptionsService, CookieOptionsService>();

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseGlobalExceptionHandler();
        app.MapControllers();

        await app.StartAsync();

        var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        return new ApiIntegrationHost(app, client, accountService);
    }

    public AuthenticationHeaderValue CreateBearerHeader(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = "Admin")
    {
        return TestAuthTokenFactory.CreateBearerHeader(userId, username, role);
    }

    public void AuthenticateAs(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = "Admin")
    {
        Client.DefaultRequestHeaders.Authorization = CreateBearerHeader(userId, username, role);
    }

    public void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    public void SetCookie(string name, string value)
    {
        _cookies[name] = value;
        ApplyCookieHeader();
    }

    public void RemoveCookie(string name)
    {
        if (_cookies.Remove(name))
        {
            ApplyCookieHeader();
        }
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (_cookies.Count > 0 && !request.Headers.Contains("Cookie"))
        {
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(static pair => $"{pair.Key}={pair.Value}")));
        }

        var response = await Client.SendAsync(request);
        CaptureCookies(response);
        return response;
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<TValue>(string url, TValue value)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(value)
        });
    }

    public Task<HttpResponseMessage> PostAsync(string url)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, url));
    }

    public bool TryGetCookie(string name, out string value)
    {
        return _cookies.TryGetValue(name, out value!);
    }

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private void ApplyCookieHeader()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        if (_cookies.Count > 0)
        {
            Client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", _cookies.Select(static pair => $"{pair.Key}={pair.Value}")));
        }
    }

    private void CaptureCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var header in values)
        {
            var firstSegment = header.Split(';', 2)[0];
            var separatorIndex = firstSegment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = firstSegment[..separatorIndex];
            var value = firstSegment[(separatorIndex + 1)..];
            if (string.IsNullOrEmpty(value))
            {
                _cookies.Remove(name);
            }
            else
            {
                _cookies[name] = value;
            }
        }

        ApplyCookieHeader();
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var headerValue = authorizationHeader.ToString();
            if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var token = headerValue["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing bearer token"));
            }

            try
            {
                var principal = TestAuthTokenFactory.CreatePrincipal(token);
                var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (FormatException exception)
            {
                return Task.FromResult(AuthenticateResult.Fail(exception));
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return Task.CompletedTask;
        }
    }
}
