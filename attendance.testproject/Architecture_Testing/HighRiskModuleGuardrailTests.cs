using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Account;

namespace attendance.testproject.Architecture_Testing;

public class HighRiskModuleGuardrailTests
{
    [Fact]
    public void AccountController_ShouldStayOnServiceBoundary()
    {
        var constructorParameters = GetSingleConstructorParameters(typeof(AccountController));
        var parameterTypes = constructorParameters.Select(parameter => parameter.ParameterType).ToList();

        Assert.Contains(parameterTypes, type => type == typeof(IAccountService));
        Assert.Contains(parameterTypes, type => type == typeof(ICookieOptionsService));
        Assert.DoesNotContain(parameterTypes, type => type.Namespace == "attendance_monitoring.IRepository");
        Assert.DoesNotContain(parameterTypes, type => type.Namespace == "attendance_monitoring.Repositories");
    }

    [Fact]
    public void AttendanceController_ShouldDependOnSingleAttendanceServiceAbstraction()
    {
        var constructorParameters = GetSingleConstructorParameters(typeof(AttendanceController));
        var parameterTypes = constructorParameters.Select(parameter => parameter.ParameterType).ToList();

        Assert.Contains(parameterTypes, type => type == typeof(IAttendanceService));
        Assert.Equal(1, parameterTypes.Count(type => type == typeof(IAttendanceService)));
        Assert.DoesNotContain(parameterTypes, type => type.Namespace == "attendance_monitoring.IRepository");
        Assert.DoesNotContain(parameterTypes, type => type.Namespace == "attendance_monitoring.Repositories");
    }

    [Fact]
    public void QrCodeController_ShouldKeepDirectRepositoryUsageToSingleSessionBoundary()
    {
        var constructorParameters = GetSingleConstructorParameters(typeof(QrCodeController));
        var repositoryParameters = constructorParameters
            .Where(parameter => parameter.ParameterType.Namespace == "attendance_monitoring.IRepository")
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.Single(repositoryParameters);
        Assert.Equal(typeof(ISessionRepository), repositoryParameters[0]);
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType.Namespace == "attendance_monitoring.Repositories");
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IQrCodeService));
    }

    [Fact]
    public void AttendanceService_ShouldUseRepositoryAbstractionsAndUserContextOnly()
    {
        var constructorParameters = GetSingleConstructorParameters(typeof(AttendanceService));
        var nonLoggerDependencyNames = constructorParameters
            .Where(parameter => !IsLogger(parameter.ParameterType))
            .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
            .ToList();

        Assert.Contains(typeof(IAttendanceRepository).FullName!, nonLoggerDependencyNames);
        Assert.Contains(typeof(IStudentRepository).FullName!, nonLoggerDependencyNames);
        Assert.Contains(typeof(IInstructorRepository).FullName!, nonLoggerDependencyNames);
        Assert.Contains(typeof(ISessionRepository).FullName!, nonLoggerDependencyNames);
        Assert.Contains(typeof(IStudentEnrollmentRepository).FullName!, nonLoggerDependencyNames);
        Assert.Contains(typeof(UserContextService).FullName!, nonLoggerDependencyNames);
        Assert.DoesNotContain(nonLoggerDependencyNames, name => name.StartsWith("attendance_monitoring.Controllers", StringComparison.Ordinal));
    }

    [Fact]
    public void AuthenticationService_ShouldRemainInternalControllerFreeUnit()
    {
        var constructorParameters = GetSingleConstructorParameters(typeof(AuthenticationService));

        Assert.True(typeof(AuthenticationService).IsNotPublic, "AuthenticationService should stay internal to the account module.");
        Assert.True(typeof(AuthenticationService).IsSealed, "AuthenticationService should stay sealed to keep its focused module contract.");
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType.Namespace == "attendance_monitoring.Controllers");
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType.Namespace == "attendance_monitoring.Repositories");
    }

    private static IReadOnlyList<System.Reflection.ParameterInfo> GetSingleConstructorParameters(Type type)
    {
        var constructors = type.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var constructor = Assert.Single(constructors);
        return constructor.GetParameters();
    }

    private static bool IsLogger(Type parameterType)
        => parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(ILogger<>);
}
