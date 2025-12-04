using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                    // If the request is for a web endpoint and contains cookies, use the cookie token
                    if (context.Request.Cookies.ContainsKey("accessToken"))
                    {
                        context.Token = context.Request.Cookies["accessToken"];
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
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

    /// <summary>
    /// Adds authorization policies to the service collection.
    /// Defines AdminPolicy, PrivilegedPolicy, and UserPolicy based on roles.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            options.AddPolicy("InstructorPolicy", policy => policy.RequireRole("Admin", "Teacher"));
            options.AddPolicy("PrivilegedPolicy", policy => policy.RequireRole("Admin", "Teacher"));
            options.AddPolicy("UserPolicy", policy => policy.RequireRole("Admin", "Teacher", "Student"));
        });

        return services;
    }
}

