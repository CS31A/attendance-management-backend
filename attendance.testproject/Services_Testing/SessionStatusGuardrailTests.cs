using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using System.Text.RegularExpressions;

namespace attendance.testproject.Services_Testing;

public class SessionStatusGuardrailTests
{
    private static readonly string[] RawSessionStatuses = SessionStatusConstants.All.ToArray();

    [Fact]
    public void SessionStatusConstants_All_ContainsExpectedCanonicalValues()
    {
        Assert.Equal(
        [
            SessionStatusConstants.NotStarted,
            SessionStatusConstants.Active,
            SessionStatusConstants.Ended,
            SessionStatusConstants.Cancelled
        ], SessionStatusConstants.All);
    }

    [Theory]
    [InlineData(SessionStatusConstants.NotStarted)]
    [InlineData(SessionStatusConstants.Active)]
    [InlineData(SessionStatusConstants.Ended)]
    [InlineData(SessionStatusConstants.Cancelled)]
    [InlineData("ACTIVE")]
    public void SessionStatusConstants_IsValid_ReturnsTrue_ForCanonicalValues(string status)
    {
        Assert.True(SessionStatusConstants.IsValid(status));
    }

    [Theory]
    [InlineData("")]
    [InlineData("paused")]
    [InlineData("teacher")]
    public void SessionStatusConstants_IsValid_ReturnsFalse_ForUnknownValues(string status)
    {
        Assert.False(SessionStatusConstants.IsValid(status));
    }

    [Fact]
    public void Session_DefaultStatus_IsNotStarted()
    {
        var session = new Session();

        Assert.Equal(SessionStatusConstants.NotStarted, session.Status);
    }

    [Fact]
    public void ProductionCode_DoesNotContainRawSessionStatusLiterals_OutsideDedicatedConstants()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var productionFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "attendance_monitoring"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith($"Constants{Path.DirectorySeparatorChar}SessionStatusConstants.cs", StringComparison.Ordinal));

        var offenders = new List<string>();

        foreach (var file in productionFiles)
        {
            var content = File.ReadAllText(file);

            foreach (var status in RawSessionStatuses)
            {
                if (Regex.IsMatch(content, $"\"{Regex.Escape(status)}\""))
                {
                    offenders.Add($"{Path.GetRelativePath(repositoryRoot, file)} => {status}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"Raw session status literals found outside SessionStatusConstants: {string.Join(", ", offenders)}");
    }
}
