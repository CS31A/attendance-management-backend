using Microsoft.OpenApi;

namespace attendance_monitoring.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring API documentation (OpenAPI/Swagger).
/// </summary>
public static class ApiDocumentationExtensions
{
    /// <summary>
    /// Adds OpenAPI and Swagger documentation services with JWT Bearer authentication support.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        // Add OpenAPI support
        services.AddOpenApi();

        // Configure Swagger with JWT Bearer authentication
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Attendance Monitoring API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        // Add endpoints API explorer
        services.AddEndpointsApiExplorer();

        return services;
    }
}

