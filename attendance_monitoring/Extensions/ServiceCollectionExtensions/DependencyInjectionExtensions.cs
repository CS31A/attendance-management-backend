using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Options;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using attendance_monitoring.Services.HealthChecks;
using attendance_monitoring.Services.Account;
using attendance_monitoring.Services.AdminData;
using attendance_monitoring.Services.QrCode;

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
        // Register ConfiguredTimeZoneProvider with fallback to system local time
        services.AddSingleton<ConfiguredTimeZoneProvider>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var settings = configuration?.GetSection(TimeZoneSettings.SectionName).Get<TimeZoneSettings>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            
            if (settings == null || string.IsNullOrWhiteSpace(settings.TimeZoneId))
            {
                // Fallback to system local time if not configured
                settings = new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id };
            }
            
            return new ConfiguredTimeZoneProvider(settings, timeProvider);
        });

        services.AddSingleton<RequestReliabilityTelemetry>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountService>(sp => new AccountService(
            sp.GetRequiredService<IRegistrationService>(),
            sp.GetRequiredService<IAuthenticationService>(),
            sp.GetRequiredService<IProfileService>(),
            sp.GetRequiredService<IAdminService>()));
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminService, AdminService>();
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
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        services.AddScoped<ICookieOptionsService, CookieOptionsService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IDataSeederService, DataSeederService>();
        services.AddScoped<IFingerprintService, FingerprintService>();
        services.AddScoped<IAdminDataService, AdminDataService>();

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
