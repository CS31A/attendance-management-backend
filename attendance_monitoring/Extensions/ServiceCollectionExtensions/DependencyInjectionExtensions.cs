using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Options;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Crud;
using attendance_monitoring.Services.HealthChecks;
using attendance_monitoring.Services.Account;
using attendance_monitoring.Services.AdminData;
using attendance_monitoring.Services.QrCode;
using attendance_monitoring.Services.InstructorServices;


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

        // Generic CRUD repositories
        services.AddScoped<IGenericCrudRepository<Classroom>, GenericCrudRepository<Classroom>>();
        services.AddScoped<IGenericCrudRepository<Subject>, GenericCrudRepository<Subject>>();
        services.AddScoped<IGenericCrudRepository<Course>, GenericCrudRepository<Course>>();
        services.AddScoped<IGenericCrudRepository<Section>, GenericCrudRepository<Section>>();

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
        services.AddScoped<IInstructorCrudService, InstructorCrudService>();
        services.AddScoped<IInstructorQueryService, InstructorQueryService>();
        services.AddScoped<IInstructorDetailService, InstructorDetailService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUserFactory, attendance_monitoring.Classes.Factory.UserFactory>();
        // Section CRUD via generic module
        services.AddScoped<CrudServiceConfig<Section, Section, Section>>(_ =>
            SectionConfig.Create());
        services.AddScoped<ICrudService<Section, Section, Section>,
            CrudService<Section, Section, Section>>();
        services.AddScoped<ISectionService, SectionService>();
        // Course CRUD via generic module
        services.AddScoped<CrudServiceConfig<Course, CreateCourse, UpdateCourse>>(sp =>
            CourseConfig.Create(sp.GetRequiredService<ICourseRepository>()));
        services.AddScoped<ICrudService<Course, CreateCourse, UpdateCourse>,
            CrudService<Course, CreateCourse, UpdateCourse>>();
        services.AddScoped<ICourseService, CourseService>();
        // Subject CRUD via generic module
        services.AddScoped<CrudServiceConfig<Subject, CreateSubject, UpdateSubject>>(sp =>
            SubjectConfig.Create(sp.GetRequiredService<ISubjectRepository>()));
        services.AddScoped<ICrudService<Subject, CreateSubject, UpdateSubject>,
            CrudService<Subject, CreateSubject, UpdateSubject>>();
        services.AddScoped<ISubjectService, SubjectService>();
        // Classroom CRUD via generic module
        services.AddScoped<CrudServiceConfig<Classroom, CreateClassroom, UpdateClassroom>>(sp =>
            ClassroomConfig.Create(sp.GetRequiredService<IClassroomRepository>()));
        services.AddScoped<ICrudService<Classroom, CreateClassroom, UpdateClassroom>,
            CrudService<Classroom, CreateClassroom, UpdateClassroom>>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<QrCodeAuthorizationService>();
        services.AddScoped<IQrCodeQueryService, QrCodeQueryService>();
        services.AddScoped<IQrCodeWriteService, QrCodeWriteService>();
        services.AddScoped<IQrCodeGenerationService, QrCodeGenerationService>();
        services.AddScoped<IQrCodeScanService, QrCodeScanService>();
        services.AddScoped<IRoleInitializationService, RoleInitializationService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        services.AddScoped<ICookieOptionsService, CookieOptionsService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<IAutomaticSessionEndService, AutomaticSessionEndService>();
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
        services.AddHostedService<AutomaticSessionEndBackgroundService>();
        
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
