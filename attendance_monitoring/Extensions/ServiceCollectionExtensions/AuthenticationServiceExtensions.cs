using attendance_monitoring.Constants;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace attendance_monitoring.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring authentication and authorization services.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication services to the service collection.
    /// Configures token validation parameters and JWT events for cookie authentication and token blacklist validation.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate JWT configuration before setting up authentication
        JwtConfigurationValidator.ValidateJwtConfiguration(configuration);

        // Get validated configuration values
        var token = JwtConfigurationValidator.GetValidatedToken(configuration);
        var issuer = JwtConfigurationValidator.GetValidatedIssuer(configuration);
        var audience = JwtConfigurationValidator.GetValidatedAudience(configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(token))
            };

            // Add cookie authentication for web login
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;

                    // For SignalR connections, check query string token first
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                    {
                        context.Token = accessToken;
                    }
                    // Otherwise fall back to cookie for web endpoints
                    else if (context.Request.Cookies.ContainsKey("accessToken"))
                    {
                        context.Token = context.Request.Cookies["accessToken"];
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    if (context.Principal != null)
                    {
                        context.Principal = NormalizeLegacyRoleClaims(context.Principal);
                    }

                    // Check if the token has been blacklisted
                    var tokenValidationService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenValidationService>();
                    var jti = context.Principal?.Claims
                        .FirstOrDefault(c => c.Type == "jti")?.Value;

                    if (!string.IsNullOrEmpty(jti) &&
                        await tokenValidationService.IsTokenBlacklistedAsync(jti))
                    {
                        // Token has been blacklisted
                        context.Fail("Token has been revoked");
                    }

                    await Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static ClaimsPrincipal NormalizeLegacyRoleClaims(ClaimsPrincipal principal)
    {
        var normalizedIdentities = principal.Identities.Select(identity =>
        {
            var claims = identity.Claims
                .Where(claim => !(claim.Type == ClaimTypes.Role &&
                                  string.Equals(claim.Value, RoleConstants.LegacyTeacher, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var hasLegacyTeacherRole = identity.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role &&
                string.Equals(claim.Value, RoleConstants.LegacyTeacher, StringComparison.OrdinalIgnoreCase));

            var hasInstructorRole = identity.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role &&
                string.Equals(claim.Value, RoleConstants.Instructor, StringComparison.OrdinalIgnoreCase));

            if (hasLegacyTeacherRole && !hasInstructorRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleConstants.Instructor));
            }

            return new ClaimsIdentity(claims, identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType);
        });

        return new ClaimsPrincipal(normalizedIdentities);
    }

    /// <summary>
    /// Adds authorization policies to the service collection.
    /// Defines AdminPolicy, InstructorPolicy, PrivilegedPolicy, StudentPolicy, and UserPolicy based on roles.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole(RoleConstants.Admin));
            options.AddPolicy("InstructorPolicy", policy => policy.RequireRole(RoleConstants.Admin, RoleConstants.Instructor));
            options.AddPolicy("PrivilegedPolicy", policy => policy.RequireRole(RoleConstants.Admin, RoleConstants.Instructor));
            options.AddPolicy("StudentPolicy", policy => policy.RequireRole(RoleConstants.Student));
            options.AddPolicy("UserPolicy", policy => policy.RequireRole(RoleConstants.Admin, RoleConstants.Instructor, RoleConstants.Student));
        });

        return services;
    }
}
