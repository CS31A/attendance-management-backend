using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;

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

        return services;
    }

    /// <summary>
    /// Registers all application service implementations with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserFactory, attendance_monitoring.Classes.Factory.UserFactory>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IRoleInitializationService, RoleInitializationService>();
        services.AddScoped<UserContextService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        services.AddScoped<ICookieOptionsService, CookieOptionsService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IDataSeederService, DataSeederService>();

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

        return services;
    }
}

