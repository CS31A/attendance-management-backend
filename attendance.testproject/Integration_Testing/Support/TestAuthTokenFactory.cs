using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace attendance.testproject.Integration_Testing.Support;

internal static class TestAuthTokenFactory
{
    private const string DefaultRole = "Admin";

    public static string CreateToken(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = DefaultRole)
    {
        var raw = $"{userId}|{username}|{role}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static AuthenticationHeaderValue CreateBearerHeader(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = DefaultRole)
    {
        return new AuthenticationHeaderValue("Bearer", CreateToken(userId, username, role));
    }

    public static string CreateAuthorizationValue(
        string userId = "integration-user",
        string username = "integration-admin",
        string role = DefaultRole)
    {
        return $"{CreateBearerHeader(userId, username, role).Scheme} {CreateToken(userId, username, role)}";
    }

    public static ClaimsPrincipal CreatePrincipal(string token)
    {
        var parts = Encoding.UTF8.GetString(Convert.FromBase64String(token)).Split('|');
        var userId = parts.ElementAtOrDefault(0) ?? "integration-user";
        var username = parts.ElementAtOrDefault(1) ?? "integration-admin";
        var role = parts.ElementAtOrDefault(2) ?? DefaultRole;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, ApiIntegrationHost.AuthenticationScheme));
    }
}
