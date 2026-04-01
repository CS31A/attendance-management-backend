using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using attendance_monitoring.Controllers;
using attendance_monitoring.Services;

namespace attendance.testproject.Architecture_Testing;

public class ControllerServiceRepositoryGuardrailTests
{
    private static readonly HashSet<string> ApprovedControllerRepositoryAdapters =
    [
        nameof(QrCodeController)
    ];

    [Fact]
    public void Controllers_ShouldNotGainNewRepositoryDependenciesOutsideApprovedAdapters()
    {
        var offenders = typeof(AccountController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .Select(type => new
            {
                type.Name,
                RepositoryDependencies = type.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Where(parameter => parameter.ParameterType.Namespace == "attendance_monitoring.IRepository")
                    .Select(parameter => parameter.ParameterType.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            })
            .Where(entry => entry.RepositoryDependencies.Count > 0)
            .Where(entry => !ApprovedControllerRepositoryAdapters.Contains(entry.Name))
            .Select(entry => $"{entry.Name} => {string.Join(", ", entry.RepositoryDependencies)}")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Controllers should not depend on repositories directly. Offending controllers: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void RepositoryImplementations_ShouldNotDependOnControllers()
    {
        var result = Types.InAssembly(typeof(SessionService).Assembly)
            .That()
            .ResideInNamespace("attendance_monitoring.Repositories")
            .ShouldNot()
            .HaveDependencyOn("attendance_monitoring.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result.FailingTypeNames ?? []));
    }

    [Fact]
    public void RepositoryImplementations_ShouldNotDependOnServices()
    {
        var result = Types.InAssembly(typeof(SessionService).Assembly)
            .That()
            .ResideInNamespace("attendance_monitoring.Repositories")
            .ShouldNot()
            .HaveDependencyOn("attendance_monitoring.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controllers_ShouldOnlyUseRepositoryInterfacesWhenAnApprovedEdgeRequiresIt()
    {
        var offenders = typeof(AccountController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .Select(type => new
            {
                type.Name,
                RepositoryDependencies = type.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Where(parameter => parameter.ParameterType.Namespace == "attendance_monitoring.IRepository")
                    .Select(parameter => parameter.ParameterType.Name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList()
            })
            .Where(entry => entry.RepositoryDependencies.Count > 0)
            .Where(entry => !ApprovedControllerRepositoryAdapters.Contains(entry.Name))
            .Select(entry => $"{entry.Name} => {string.Join(", ", entry.RepositoryDependencies)}")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Controllers must stay on the controller-service boundary. Offending constructors: {string.Join(", ", offenders)}");
    }

    private static string FormatFailure(IEnumerable<string> failingTypes)
    {
        var failures = failingTypes.ToList();
        return failures.Count == 0
            ? "Architecture rule failed."
            : $"Architecture rule failed. Offending types: {string.Join(", ", failures)}";
    }
}
