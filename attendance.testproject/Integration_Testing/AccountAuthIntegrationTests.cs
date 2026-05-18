using System.Net;
using System.Net.Http.Json;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.Account;
using attendance.testproject.Integration_Testing.Support;
using Microsoft.AspNetCore.Http;
using IAuthenticationService = attendance_monitoring.Services.Account.IAuthenticationService;

namespace attendance.testproject.Integration_Testing;

public sealed class AccountAuthIntegrationTests
{
    [Fact]
    public async Task PostApiAccountLogin_ReturnsOk_WithApprovedBearerPayload()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        var loginDto = new LoginDto { Username = "admin", Password = "Password123!" };

        host.AuthenticationService
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

        // POST /api/account/login
        var response = await host.PostAsJsonAsync("/api/account/login", loginDto);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Login successful", payload.Message);
        Assert.Equal("access-token-1", payload.AccessToken);
        Assert.Equal("refresh-token-1", payload.RefreshToken);
        Assert.Equal("admin", payload.User);
        Assert.Equal("Admin", payload.Role);
    }

    [Fact]
    public async Task PostApiAccountRefresh_ReturnsOk_WithRotatedTokens()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        var refreshRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "refresh-token-1",
            OldAccessToken = "access-token-1"
        };

        host.AuthenticationService
            .Setup(service => service.RefreshAsync(It.Is<RefreshTokenRequestDto>(request =>
                request.RefreshToken == refreshRequest.RefreshToken &&
                request.OldAccessToken == refreshRequest.OldAccessToken)))
            .ReturnsAsync(new TokenResponseDto
            {
                AccessToken = "access-token-2",
                RefreshToken = "refresh-token-2"
            });

        // POST /api/account/refresh
        var response = await host.PostAsJsonAsync("/api/account/refresh", refreshRequest);
        var payload = await response.Content.ReadFromJsonAsync<RefreshResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Token refreshed successfully", payload.Message);
        Assert.Equal("access-token-2", payload.AccessToken);
        Assert.Equal("refresh-token-2", payload.RefreshToken);
    }

    [Fact]
    public async Task PostApiAccountRevoke_ReturnsOk_ForAuthenticatedBearerRequest()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "refresh-token-2" };
        host.AuthenticateAs(userId: "user-42", username: "admin", role: "Admin");

        host.AuthenticationService
            .Setup(service => service.RevokeAsync(
                It.Is<RevokeTokenRequestDto>(request => request.RefreshToken == revokeRequest.RefreshToken),
                "user-42"))
            .ReturnsAsync(new RevokeResponseDto
            {
                Success = true,
                Message = "Refresh token revoked successfully"
            });

        // POST /api/account/revoke
        var response = await host.PostAsJsonAsync("/api/account/revoke", revokeRequest);
        var payload = await response.Content.ReadFromJsonAsync<RevokeResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Refresh token revoked successfully", payload.Message);
    }

    [Fact]
    public async Task PostApiAccountLogout_ReturnsOk_ForAuthenticatedBearerRequest()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        var bearerToken = TestAuthTokenFactory.CreateToken("user-42", "admin", "Admin");
        host.AuthenticateAs(userId: "user-42", username: "admin", role: "Admin");

        host.AuthenticationService
            .Setup(service => service.LogoutAsync("user-42", bearerToken))
            .ReturnsAsync(new LogoutResponseDto
            {
                Success = true,
                Message = "Logged out successfully"
            });

        // POST /api/account/logout
        var response = await host.PostAsync("/api/account/logout");
        var payload = await response.Content.ReadFromJsonAsync<LogoutResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Logged out successfully", payload.Message);
    }

    [Fact]
    public async Task PostApiAccountWebLogin_ReturnsOk_AndSetsCookies()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        var webLoginDto = new WebLoginDto { Identifier = "admin@example.com", Password = "Password123!" };

        host.AuthenticationService
            .Setup(service => service.LoginAsync(It.Is<LoginDto>(request =>
                request.Username == webLoginDto.Identifier &&
                request.Password == webLoginDto.Password)))
            .ReturnsAsync(new LoginResult
            {
                TokenResponse = new TokenResponseDto
                {
                    AccessToken = "cookie-access-token",
                    RefreshToken = "cookie-refresh-token"
                },
                Username = "admin",
                Role = "Admin"
            });

        // POST /api/account/web/login
        var response = await host.PostAsJsonAsync("/api/account/web/login", webLoginDto);
        var payload = await response.Content.ReadFromJsonAsync<WebLoginResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Login successful", payload.Message);
        Assert.Equal("admin", payload.Username);
        Assert.Equal("Admin", payload.Role);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
        Assert.Contains(setCookieValues, header => header.Contains("accessToken=", StringComparison.Ordinal));
        Assert.Contains(setCookieValues, header => header.Contains("refreshToken=", StringComparison.Ordinal));
        Assert.True(host.TryGetCookie("accessToken", out var accessToken));
        Assert.True(host.TryGetCookie("refreshToken", out var refreshToken));
        Assert.Equal("cookie-access-token", accessToken);
        Assert.Equal("cookie-refresh-token", refreshToken);
    }

    [Fact]
    public async Task PostApiAccountWebRefresh_ReturnsOk_AndRotatesCookies()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.SetCookie("accessToken", "current-access-token");
        host.SetCookie("refreshToken", "current-refresh-token");

        host.AuthenticationService
            .Setup(service => service.RefreshAsync(It.Is<RefreshTokenRequestDto>(request =>
                request.RefreshToken == "current-refresh-token" &&
                request.OldAccessToken == "current-access-token")))
            .ReturnsAsync(new TokenResponseDto
            {
                AccessToken = "next-access-token",
                RefreshToken = "next-refresh-token"
            });

        // POST /api/account/web/refresh
        var response = await host.PostAsync("/api/account/web/refresh");
        var payload = await response.Content.ReadFromJsonAsync<WebRefreshResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Tokens refreshed successfully", payload.Message);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
        Assert.Contains(setCookieValues, header => header.Contains("accessToken=", StringComparison.Ordinal));
        Assert.Contains(setCookieValues, header => header.Contains("refreshToken=", StringComparison.Ordinal));
        Assert.True(host.TryGetCookie("accessToken", out var accessToken));
        Assert.True(host.TryGetCookie("refreshToken", out var refreshToken));
        Assert.Equal("next-access-token", accessToken);
        Assert.Equal("next-refresh-token", refreshToken);
    }

    [Fact]
    public async Task PostApiAccountWebLogout_ReturnsOk_AndClearsCookies()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-42", username: "admin", role: "Admin");
        host.SetCookie("accessToken", "current-access-token");
        host.SetCookie("refreshToken", "current-refresh-token");

        host.AuthenticationService
            .Setup(service => service.WebLogoutAsync("user-42", "current-access-token"))
            .ReturnsAsync(new LogoutResponseDto
            {
                Success = true,
                Message = "Logged out successfully"
            });

        // POST /api/account/web/logout
        var response = await host.PostAsync("/api/account/web/logout");
        var payload = await response.Content.ReadFromJsonAsync<LogoutResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Logged out successfully", payload.Message);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
        Assert.Contains(setCookieValues, header => header.Contains("accessToken=;", StringComparison.Ordinal));
        Assert.Contains(setCookieValues, header => header.Contains("refreshToken=;", StringComparison.Ordinal));
        Assert.False(host.TryGetCookie("accessToken", out _));
        Assert.False(host.TryGetCookie("refreshToken", out _));
    }

    [Fact]
    public async Task PostApiAccountLogin_ReturnsUnauthorized_ForInvalidCredentials()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticationService
            .Setup(service => service.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new ValidationException("Invalid email or username or password"));

        var response = await host.PostAsJsonAsync("/api/account/login", new LoginDto
        {
            Username = "admin",
            Password = "wrong-password"
        });
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.Equal("Invalid email or username or password", payload.Message);
    }

    [Fact]
    public async Task PostApiAccountLogin_ReturnsErrorResponseDto_ForUnexpectedFailures()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticationService
            .Setup(service => service.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        var response = await host.PostAsJsonAsync("/api/account/login", new LoginDto
        {
            Username = "admin",
            Password = "Password123!"
        });
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.Equal(StatusCodes.Status500InternalServerError, payload.StatusCode);
        Assert.Equal("/api/account/login", payload.Path);
        Assert.Equal(
            "An unexpected error occurred. Please contact support if this persists.",
            payload.Message);
        Assert.Null(payload.Details);
        Assert.True(payload.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task PostApiAccountWebRefresh_ReturnsUnauthorized_WhenRefreshCookieIsMissing()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();

        var response = await host.PostAsync("/api/account/web/refresh");
        var payload = await response.Content.ReadFromJsonAsync<WebRefreshResponseDto>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.Equal("Refresh token not found", payload.Message);
    }
}
