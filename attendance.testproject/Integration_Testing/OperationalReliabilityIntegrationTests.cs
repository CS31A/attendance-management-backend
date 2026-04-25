using System.Net;
using System.Net.Http.Json;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

public sealed class OperationalReliabilityIntegrationTests
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private const string ReliabilityMeterName = "attendance_monitoring.reliability";
    private const string RequestDurationInstrumentName = "request.duration.ms";
    private const string RequestErrorsInstrumentName = "request.errors";

    [Fact]
    public async Task PostApiAccountLogin_PreservesValidInboundCorrelationId()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);
        var inboundCorrelationId = "auth-login-correlation-123";
        var loginDto = new LoginDto { Username = "admin", Password = "Password123!" };

        host.AccountService
            .Setup(service => service.LoginAsync(It.Is<LoginDto>(request =>
                request.Username == loginDto.Username &&
                request.Password == loginDto.Password)))
            .ReturnsAsync(new LoginResult
            {
                TokenResponse = new TokenResponseDto
                {
                    AccessToken = "access-token-1",
                    RefreshToken = "refresh-token-1"
                },
                Username = "admin",
                Role = "Admin"
            });

        var request = CreateJsonPostRequest("/api/account/login", loginDto, inboundCorrelationId);

        var response = await host.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(inboundCorrelationId, GetSingleHeaderValue(response, CorrelationHeaderName));
    }

    [Fact]
    public async Task PostApiAttendance_ReplacesInvalidInboundCorrelationId()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");
        const string invalidCorrelationId = "invalid correlation id!";
        var identifiers = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) => new
        {
            StudentId = await dbContext.Students
                .Where(student => student.Id == host.AttendanceQrScenario.StudentId)
                .Select(student => student.Id)
                .SingleAsync(cancellationToken),
            SessionId = await dbContext.Sessions
                .Where(session => session.Id == host.AttendanceQrScenario.SessionId)
                .Select(session => session.Id)
                .SingleAsync(cancellationToken)
        });

        var request = CreateJsonPostRequest("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = identifiers.StudentId,
            SessionId = identifiers.SessionId,
            Status = "Present",
            Notes = "Correlation validation"
        }, invalidCorrelationId);

        var response = await host.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var effectiveCorrelationId = GetSingleHeaderValue(response, CorrelationHeaderName);
        Assert.False(string.IsNullOrWhiteSpace(effectiveCorrelationId));
        Assert.NotEqual(invalidCorrelationId, effectiveCorrelationId);
    }

    [Fact]
    public async Task PostApiQrCodeScan_ReturnsCorrelationId_WhenRequestOmitsHeader()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.StudentUserId, username: "integration-student", role: "Student");

        var response = await host.PostAsJsonAsync("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(GetSingleHeaderValue(response, CorrelationHeaderName)));
    }

    [Fact]
    public async Task PostApiAccountLogin_EmitsResponseTimeHeader()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);

        var response = await SendAuthSuccessAsync(host);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var headerValue = GetSingleHeaderValue(response, "X-Response-Time");
        Assert.EndsWith("ms", headerValue, StringComparison.Ordinal);
        Assert.True(double.TryParse(
            headerValue[..^2],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var elapsedMilliseconds));
        Assert.True(elapsedMilliseconds >= 0d);
        Assert.True(elapsedMilliseconds < 5000d, $"Response time {elapsedMilliseconds}ms was suspiciously high.");
    }

    [Fact]
    public async Task PostApiAccountLogin_ErrorResponse_UsesSameEffectiveCorrelationId()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);
        var inboundCorrelationId = "auth-error-correlation-456";

        host.AccountService
            .Setup(service => service.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        var request = CreateJsonPostRequest("/api/account/login", new LoginDto
        {
            Username = "admin",
            Password = "Password123!"
        }, inboundCorrelationId);

        var response = await host.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(inboundCorrelationId, GetSingleHeaderValue(response, CorrelationHeaderName));
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("attendance")]
    [InlineData("qr")]
    public async Task TrackedEndpointGroups_EmitLatencyMeasurements_ForSuccessfulRequests(string endpointGroup)
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);

        var response = endpointGroup switch
        {
            "auth" => await SendAuthSuccessAsync(host),
            "attendance" => await SendAttendanceSuccessAsync(host),
            "qr" => await SendQrSuccessAsync(host),
            _ => throw new ArgumentOutOfRangeException(nameof(endpointGroup), endpointGroup, "Unsupported endpoint group.")
        };

        Assert.True(response.IsSuccessStatusCode);

        var durationMeasurements = host.Telemetry.GetMeasurements(RequestDurationInstrumentName);
        Assert.Contains(durationMeasurements, measurement =>
            measurement.NumericValue >= 0d &&
            string.Equals(measurement.GetTagValue("endpoint_group"), endpointGroup, StringComparison.Ordinal));

        Assert.DoesNotContain(host.Telemetry.GetMeasurements(RequestErrorsInstrumentName), measurement =>
            string.Equals(measurement.GetTagValue("endpoint_group"), endpointGroup, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("attendance")]
    [InlineData("qr")]
    public async Task TrackedEndpointGroups_EmitErrorMeasurements_OnlyForFailingResponses(string endpointGroup)
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);

        var response = endpointGroup switch
        {
            "auth" => await SendAuthFailureAsync(host),
            "attendance" => await SendAttendanceFailureAsync(host),
            "qr" => await SendQrFailureAsync(host),
            _ => throw new ArgumentOutOfRangeException(nameof(endpointGroup), endpointGroup, "Unsupported endpoint group.")
        };

        Assert.True((int)response.StatusCode >= 400);

        var durationMeasurements = host.Telemetry.GetMeasurements(RequestDurationInstrumentName);
        Assert.Contains(durationMeasurements, measurement =>
            string.Equals(measurement.GetTagValue("endpoint_group"), endpointGroup, StringComparison.Ordinal));

        var errorMeasurements = host.Telemetry.GetMeasurements(RequestErrorsInstrumentName);
        Assert.Contains(errorMeasurements, measurement =>
            measurement.NumericValue > 0d &&
            string.Equals(measurement.GetTagValue("endpoint_group"), endpointGroup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationalReliabilityHost_CollectsMeasurements_FromInProcessReliabilityMeter()
    {
        await using var host = await ApiIntegrationHost.CreateOperationalReliabilityAsync(AttendanceQrSeedData.ValidQrScan);

        await SendAuthSuccessAsync(host);

        Assert.NotEmpty(host.Telemetry.Measurements);
        Assert.Contains(host.Telemetry.Measurements, measurement =>
            measurement.InstrumentName is RequestDurationInstrumentName or RequestErrorsInstrumentName);
    }

    [Fact]
    public void ReliabilityTestSupport_TargetsTheExpectedApplicationMeterName()
    {
        Assert.Equal(ReliabilityMeterName, "attendance_monitoring.reliability");
    }

    private static HttpRequestMessage CreateJsonPostRequest<TValue>(string url, TValue value, string? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(value)
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add(CorrelationHeaderName, correlationId);
        }

        return request;
    }

    private static string GetSingleHeaderValue(HttpResponseMessage response, string headerName)
    {
        Assert.True(
            response.Headers.TryGetValues(headerName, out var values),
            $"Expected response header '{headerName}' to be present.");

        return Assert.Single(values);
    }

    private static Task<HttpResponseMessage> SendAuthSuccessAsync(ApiIntegrationHost host)
    {
        var loginDto = new LoginDto { Username = "admin", Password = "Password123!" };
        host.AccountService
            .Setup(service => service.LoginAsync(It.Is<LoginDto>(request =>
                request.Username == loginDto.Username &&
                request.Password == loginDto.Password)))
            .ReturnsAsync(new LoginResult
            {
                TokenResponse = new TokenResponseDto
                {
                    AccessToken = "access-token-1",
                    RefreshToken = "refresh-token-1"
                },
                Username = "admin",
                Role = "Admin"
            });

        return host.SendAsync(CreateJsonPostRequest("/api/account/login", loginDto));
    }

    private static Task<HttpResponseMessage> SendAuthFailureAsync(ApiIntegrationHost host)
    {
        host.AccountService
            .Setup(service => service.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        return host.SendAsync(CreateJsonPostRequest("/api/account/login", new LoginDto
        {
            Username = "admin",
            Password = "Password123!"
        }));
    }

    private static async Task<HttpResponseMessage> SendAttendanceSuccessAsync(ApiIntegrationHost host)
    {
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");
        var identifiers = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) => new
        {
            StudentId = await dbContext.Students.Where(student => student.Id == host.AttendanceQrScenario.StudentId).Select(student => student.Id).SingleAsync(cancellationToken),
            SessionId = await dbContext.Sessions.Where(session => session.Id == host.AttendanceQrScenario.SessionId).Select(session => session.Id).SingleAsync(cancellationToken)
        });

        return await host.SendAsync(CreateJsonPostRequest("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = identifiers.StudentId,
            SessionId = identifiers.SessionId,
            Status = "Present",
            Notes = "Telemetry success"
        }));
    }

    private static async Task<HttpResponseMessage> SendAttendanceFailureAsync(ApiIntegrationHost host)
    {
        host.ClearAuthentication();
        var identifiers = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) => new
        {
            StudentId = await dbContext.Students.Where(student => student.Id == host.AttendanceQrScenario!.StudentId).Select(student => student.Id).SingleAsync(cancellationToken),
            SessionId = await dbContext.Sessions.Where(session => session.Id == host.AttendanceQrScenario.SessionId).Select(session => session.Id).SingleAsync(cancellationToken)
        });

        return await host.SendAsync(CreateJsonPostRequest("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = identifiers.StudentId,
            SessionId = identifiers.SessionId,
            Status = "Present",
            Notes = "Telemetry failure"
        }));
    }

    private static Task<HttpResponseMessage> SendQrSuccessAsync(ApiIntegrationHost host)
    {
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.StudentUserId, username: "integration-student", role: "Student");

        return host.SendAsync(CreateJsonPostRequest("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash
        }));
    }

    private static Task<HttpResponseMessage> SendQrFailureAsync(ApiIntegrationHost host)
    {
        host.ClearAuthentication();

        return host.SendAsync(CreateJsonPostRequest("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario!.QrHash
        }));
    }
}
