using NetArchTest.Rules;
using attendance_monitoring.Services;

namespace attendance.testproject.Architecture_Testing;

public class ServiceArchitectureGuardrailTests
{
    private static readonly HashSet<string> ServiceLineBudgetAllowlist =
    [
        nameof(AttendanceService),
        nameof(DataSeederService),
        nameof(FingerprintService),
        nameof(InstructorService),
        nameof(ScheduleService),
        nameof(SessionService),
        nameof(StudentService)
    ];

    private static readonly HashSet<string> ConstructorBudgetAllowlist =
    [
        nameof(AttendanceService),
        nameof(FingerprintService),
        nameof(SessionService)
    ];

    [Fact]
    public void TopLevelServices_ShouldNotDependOnControllers()
    {
        var result = Types.InAssembly(typeof(SessionService).Assembly)
            .That()
            .ResideInNamespace("attendance_monitoring.Services")
            .And()
            .HaveNameEndingWith("Service")
            .ShouldNot()
            .HaveDependencyOn("attendance_monitoring.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Services_ShouldNotDependOnConcreteRepositoryImplementations()
    {
        var result = Types.InAssembly(typeof(SessionService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("attendance_monitoring.Services")
            .ShouldNot()
            .HaveDependencyOn("attendance_monitoring.Repositories")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result.FailingTypeNames ?? []));
    }

    [Fact]
    public void NewTopLevelServiceSourceFiles_ShouldStayWithinLineBudget()
    {
        const int maxLines = 500;
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var serviceFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "attendance_monitoring", "Services"), "*Service.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !ServiceLineBudgetAllowlist.Contains(Path.GetFileNameWithoutExtension(path)));

        var offenders = serviceFiles
            .Select(path => new
            {
                File = Path.GetRelativePath(repositoryRoot, path),
                LineCount = File.ReadLines(path).Count()
            })
            .Where(entry => entry.LineCount > maxLines)
            .Select(entry => $"{entry.File} => {entry.LineCount} lines")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Top-level service source files exceeded the {maxLines}-line budget: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NewTopLevelServices_ShouldStayWithinConstructorDependencyBudget()
    {
        const int maxConstructorDependencies = 8;

        var offenders = typeof(SessionService).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.Namespace == "attendance_monitoring.Services")
            .Where(type => type.Name.EndsWith("Service", StringComparison.Ordinal))
            .Where(type => !ConstructorBudgetAllowlist.Contains(type.Name))
            .Select(type => new
            {
                type.Name,
                DependencyCount = type.GetConstructors()
                    .Select(constructor => constructor.GetParameters().Length)
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .Where(entry => entry.DependencyCount > maxConstructorDependencies)
            .Select(entry => $"{entry.Name} => {entry.DependencyCount} constructor dependencies")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Top-level services exceeded the {maxConstructorDependencies}-dependency constructor budget: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void FingerprintAttendanceTransaction_ShouldUseExecutionStrategy()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var source = File.ReadAllText(Path.Combine(repositoryRoot, "attendance_monitoring", "Services", "FingerprintService.cs"));

        Assert.Contains("ScanFingerprintBySensorAsync", source);
        Assert.Contains("CreateExecutionStrategy", source);
    }

    private static string FormatFailure(IEnumerable<string> failingTypes)
    {
        var failures = failingTypes.ToList();
        return failures.Count == 0
            ? "Architecture rule failed."
            : $"Architecture rule failed. Offending types: {string.Join(", ", failures)}";
    }
}
