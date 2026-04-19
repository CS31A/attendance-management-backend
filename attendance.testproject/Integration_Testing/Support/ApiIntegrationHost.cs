using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using attendance_monitoring.Controllers;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Integration_Testing.Support;

internal sealed class ApiIntegrationHost : IAsyncDisposable
{
    internal const string AuthenticationScheme = "IntegrationTestBearer";
    private const string ReliabilityMeterName = "attendance_monitoring.reliability";

    private readonly WebApplication _app;
    private readonly SqliteConnection? _sqliteConnection;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);
    private readonly ReliabilityTelemetryCollector? _telemetryCollector;

    private ApiIntegrationHost(
        WebApplication app,
        HttpClient client,
        Mock<IAccountService> accountService,
        SqliteConnection? sqliteConnection = null,
        ReliabilityTelemetryCollector? telemetryCollector = null)
    {
        _app = app;
        Client = client;
        AccountService = accountService;
        _sqliteConnection = sqliteConnection;
        _telemetryCollector = telemetryCollector;
    }

    public HttpClient Client { get; }

    public Mock<IAccountService> AccountService { get; }

    public AttendanceQrScenarioContext? AttendanceQrScenario { get; private set; }

    public ReportsScenarioContext? ReportsScenario { get; private set; }

    public AccountScenarioContext? AccountScenario { get; private set; }

    public IServiceProvider Services => _app.Services;

    public ReliabilityTelemetryCollector Telemetry => _telemetryCollector
        ?? throw new InvalidOperationException("Reliability telemetry collection is not configured for this host.");

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
            ["CookieSettings:RefreshTokenExpirationDays"] = "7",
            ["TimeZoneSettings:TimeZoneId"] = TimeZoneInfo.Local.Id
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

    public static async Task<ApiIntegrationHost> CreateAttendanceQrAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var connectionString = $"Data Source=file:attendance-integration-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CookieSettings:AccessTokenExpirationMinutes"] = "15",
            ["CookieSettings:RefreshTokenExpirationDays"] = "7",
            ["CorsSettings:AllowedOrigins"] = "https://localhost",
            ["TimeZoneSettings:TimeZoneId"] = TimeZoneInfo.Local.Id
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
                options.DefaultScheme = AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(AuthenticationScheme, _ => { });
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = PathString.Empty;
            options.AccessDeniedPath = PathString.Empty;
        });
        builder.Services.AddAuthorizationPolicies();
        builder.Services.AddResponseHandling();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddSignalRServices();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new AssemblyPart(typeof(AttendanceController).Assembly));
            });

        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        builder.Services.AddSingleton(accountService);
        builder.Services.AddSingleton<IAccountService>(sp => sp.GetRequiredService<Mock<IAccountService>>().Object);

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseGlobalExceptionHandler();
        app.MapControllers();

        await app.StartAsync(cancellationToken);

        var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var host = new ApiIntegrationHost(app, client, accountService, sqliteConnection: connection);
        await host.LoadAttendanceQrScenarioAsync(scenarioName, cancellationToken);
        return host;
    }

    public static async Task<ApiIntegrationHost> CreateOperationalReliabilityAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var connectionString = $"Data Source=file:operational-reliability-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var telemetryCollector = new ReliabilityTelemetryCollector(ReliabilityMeterName);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CookieSettings:AccessTokenExpirationMinutes"] = "15",
            ["CookieSettings:RefreshTokenExpirationDays"] = "7",
            ["CorsSettings:AllowedOrigins"] = "https://localhost",
            ["TimeZoneSettings:TimeZoneId"] = TimeZoneInfo.Local.Id
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
                options.DefaultScheme = AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(AuthenticationScheme, _ => { });
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = PathString.Empty;
            options.AccessDeniedPath = PathString.Empty;
        });
        builder.Services.AddAuthorizationPolicies();
        builder.Services.AddResponseHandling();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddSignalRServices();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new AssemblyPart(typeof(AccountController).Assembly));
            });

        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        builder.Services.AddSingleton(accountService);
        builder.Services.AddSingleton<IAccountService>(sp => sp.GetRequiredService<Mock<IAccountService>>().Object);
        builder.Services.AddScoped<ICookieOptionsService, CookieOptionsService>();

        var app = builder.Build();
        app.UseSelectiveResponseCompression();
        app.UsePerformanceMonitoring();
        app.UseCorePipeline();
        app.UseGlobalExceptionHandler();

        await app.StartAsync(cancellationToken);

        var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var host = new ApiIntegrationHost(app, client, accountService, sqliteConnection: connection, telemetryCollector: telemetryCollector);
        await host.LoadAttendanceQrScenarioAsync(scenarioName, cancellationToken);
        return host;
    }

    public static async Task<ApiIntegrationHost> CreateReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var connectionString = $"Data Source=file:reports-integration-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CookieSettings:AccessTokenExpirationMinutes"] = "15",
            ["CookieSettings:RefreshTokenExpirationDays"] = "7",
            ["CorsSettings:AllowedOrigins"] = "https://localhost",
            ["Jwt:Token"] = "test-secret-key-for-integration-testing-minimum-32-characters",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["TimeZoneSettings:TimeZoneId"] = TimeZoneInfo.Local.Id
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
                options.DefaultScheme = AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(AuthenticationScheme, _ => { });
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = PathString.Empty;
            options.AccessDeniedPath = PathString.Empty;
        });
        builder.Services.AddAuthorizationPolicies();
        builder.Services.AddResponseHandling();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddSignalRServices();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new AssemblyPart(typeof(ReportsController).Assembly));
                manager.ApplicationParts.Add(new AssemblyPart(typeof(AccountController).Assembly));
            });

        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        builder.Services.AddSingleton(accountService);
        builder.Services.AddSingleton<IAccountService>(sp => sp.GetRequiredService<Mock<IAccountService>>().Object);

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseGlobalExceptionHandler();
        app.MapControllers();

        await app.StartAsync(cancellationToken);

        var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");

        var host = new ApiIntegrationHost(app, client, accountService, sqliteConnection: connection);
        await host.LoadReportsScenarioAsync(cancellationToken);
        return host;
    }

    public void AuthenticateAs(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = "Admin")
    {
        Client.DefaultRequestHeaders.Authorization = TestAuthTokenFactory.CreateBearerHeader(userId, username, role);
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

    public async Task<AttendanceQrScenarioContext> LoadAttendanceQrScenarioAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        AttendanceQrScenario = await AttendanceQrSeedData.SeedScenarioAsync(dbContext, scenarioName, cancellationToken);
        return AttendanceQrScenario;
    }

    public async Task<ReportsScenarioContext> LoadReportsScenarioAsync(
        CancellationToken cancellationToken = default)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ReportsScenario = await ReportsSeedData.SeedScenarioAsync(dbContext, cancellationToken);
        return ReportsScenario;
    }

    public async Task<AccountScenarioContext> LoadAccountScenarioAsync(
        string role,
        string initialPassword,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        AccountScenario = await AccountSeedData.SeedUserAsync(
            dbContext, userManager, roleManager, role, initialPassword, cancellationToken);
        return AccountScenario;
    }

    public async Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        Client.Dispose();
        await _app.DisposeAsync();
        _telemetryCollector?.Dispose();
        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.DisposeAsync();
        }
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

internal sealed class ReliabilityTelemetryCollector : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<TelemetryMeasurement> _measurements = new();
    private readonly string _meterName;

    public ReliabilityTelemetryCollector(string meterName)
    {
        _meterName = meterName;
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (string.Equals(instrument.Meter.Name, _meterName, StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>(RecordMeasurement);
        _listener.SetMeasurementEventCallback<long>(RecordMeasurement);
        _listener.Start();
    }

    public IReadOnlyCollection<TelemetryMeasurement> Measurements => _measurements.ToArray();

    public IReadOnlyCollection<TelemetryMeasurement> GetMeasurements(string instrumentName)
    {
        return _measurements
            .Where(measurement => string.Equals(measurement.InstrumentName, instrumentName, StringComparison.Ordinal))
            .ToArray();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private void RecordMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        _measurements.Enqueue(new TelemetryMeasurement(instrument.Name, Convert.ToDouble(measurement), tags.ToArray()));
    }
}

internal sealed record TelemetryMeasurement(
    string InstrumentName,
    double NumericValue,
    IReadOnlyList<KeyValuePair<string, object?>> Tags)
{
    public string? GetTagValue(string tagName)
    {
        return Tags.FirstOrDefault(tag => string.Equals(tag.Key, tagName, StringComparison.Ordinal)).Value?.ToString();
    }
}
