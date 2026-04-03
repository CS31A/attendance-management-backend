using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Account;
using attendance_monitoring.Services.QrCode;
using attendance_monitoring.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace attendance_monitoring.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring dependency injection (repositories, services, and background services).
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers all repository implementations with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IClassroomRepository, ClassroomRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IQrCodeRepository, QrCodeRepository>();
        services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IFingerprintRepository, FingerprintRepository>();

        return services;
    }

    /// <summary>
    /// Registers all application service implementations with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<RequestReliabilityTelemetry>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountService>(sp => new AccountService(
            sp.GetRequiredService<RegistrationService>(),
            sp.GetRequiredService<AuthenticationService>(),
            sp.GetRequiredService<ProfileService>(),
            sp.GetRequiredService<AdminService>()));
        services.AddScoped<RegistrationService>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<AdminService>();
        services.AddScoped<IUserFactory, attendance_monitoring.Classes.Factory.UserFactory>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IQrCodeService>(sp => new QrCodeService(
            sp.GetRequiredService<QrCodeQueryService>(),
            sp.GetRequiredService<QrCodeWriteService>(),
            sp.GetRequiredService<QrCodeGenerationService>(),
            sp.GetRequiredService<QrCodeScanService>()));
        services.AddScoped<QrCodeAuthorizationService>();
        services.AddScoped<QrCodeQueryService>();
        services.AddScoped<QrCodeWriteService>();
        services.AddScoped<QrCodeGenerationService>();
        services.AddScoped<QrCodeScanService>();
        services.AddScoped<IRoleInitializationService, RoleInitializationService>();
        services.AddScoped<UserContextService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        services.AddScoped<ICookieOptionsService, CookieOptionsService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IDataSeederService, DataSeederService>();
        services.AddScoped<IFingerprintService, FingerprintService>();

        // SignalR and Notification services
        services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
        services.AddSingleton<INotificationPreferenceService, InMemoryPreferenceService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    /// <summary>
    /// Registers background/hosted services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<BlacklistedTokenCleanupService>();
        services.AddHostedService<RoleInitializationBackgroundService>();
        
        // Orphaned user cleanup and monitoring service
        services.AddSingleton<OrphanedUserCleanupService>();
        services.AddHostedService(provider => provider.GetRequiredService<OrphanedUserCleanupService>());
        services.AddSingleton<IOrphanedUserCleanupService>(provider => provider.GetRequiredService<OrphanedUserCleanupService>());

        return services;
    }

    /// <summary>
    /// Registers health checks for monitoring application status.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseConnectivityHealthCheck>(
                "database",
                tags: ["ready"])
            .AddCheck<DataIntegrityHealthCheck>(
                "data_integrity",
                tags: ["ready", "data-integrity"]);

        return services;
    }
}

/// <summary>
/// Health check for verifying database connectivity for readiness evaluation.
/// </summary>
internal sealed class DatabaseConnectivityHealthCheck(ApplicationDbContext applicationDbContext) : IHealthCheck
{
    /// <summary>
    /// Verifies that the application database is reachable.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await applicationDbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database connectivity is healthy.", new Dictionary<string, object> { ["connected"] = true })
                : HealthCheckResult.Unhealthy(
                    "Database connection failed.",
                    data: new Dictionary<string, object>
                    {
                        ["connected"] = false,
                        ["error"] = "Database connection failed"
                    });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed.",
                ex,
                new Dictionary<string, object>
                {
                    ["connected"] = false,
                    ["error"] = ex.Message
                });
        }
    }
}
