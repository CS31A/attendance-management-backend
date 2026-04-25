using System.Text.RegularExpressions;

namespace attendance.testproject.Architecture_Testing;

public class UuidMigrationGuardrailTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static readonly string[] MigrationRelativePaths =
    [
        Path.Combine("attendance_monitoring", "Migrations", "20260421182220_AddWave1ProfileUuidColumns.cs"),
        Path.Combine("attendance_monitoring", "Migrations", "20260422130928_AddSliceAAcademicUuidColumns.cs"),
        Path.Combine("attendance_monitoring", "Migrations", "20260422132104_AddSliceBAttendanceUuidColumns.cs"),
        Path.Combine("attendance_monitoring", "Migrations", "20260422145611_AddFingerprintSupportUuidColumns.cs")
    ];

    private static readonly string[] RequiredMigrationPatterns =
    [
        "WHERE [Uuid] IS NULL",
        "THROW",
        "NotSupportedException"
    ];

    private static readonly string[] RequiredSnapshotIndexNames =
    [
        "IX_Courses_Uuid",
        "IX_Subjects_Uuid",
        "IX_Sections_Uuid",
        "IX_Classrooms_Uuid",
        "IX_Schedules_Uuid",
        "IX_StudentEnrollments_Uuid",
        "IX_Sessions_Uuid",
        "IX_AttendanceRecords_Uuid",
        "IX_QrCodes_Uuid",
        "IX_Fingerprints_Uuid",
        "IX_FingerprintDevices_Uuid",
        "IX_FingerprintEnrollmentSessions_Uuid",
        "IX_FingerprintScanEvents_Uuid"
    ];

    [Fact]
    public void UuidMigrationSources_ShouldRemainForwardOnlyAndAnomalyGuarded()
    {
        var offenders = new List<string>();

        foreach (var relativePath in MigrationRelativePaths)
        {
            var fullPath = Path.Combine(RepositoryRoot, relativePath);
            var content = File.ReadAllText(fullPath);

            foreach (var pattern in RequiredMigrationPatterns)
            {
                if (!content.Contains(pattern, StringComparison.Ordinal))
                {
                    offenders.Add($"{relativePath} missing '{pattern}'");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"UUID migration guardrails missing expected patterns: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ApplicationDbContextModelSnapshot_ShouldContainAllWidenedUuidIndexes()
    {
        var snapshotRelativePath = Path.Combine("attendance_monitoring", "Migrations", "ApplicationDbContextModelSnapshot.cs");
        var snapshotPath = Path.Combine(RepositoryRoot, snapshotRelativePath);
        var snapshotContent = File.ReadAllText(snapshotPath);

        var missingIndexes = RequiredSnapshotIndexNames
            .Where(indexName => !Regex.IsMatch(snapshotContent, Regex.Escape(indexName)))
            .ToList();

        Assert.True(missingIndexes.Count == 0,
            $"Model snapshot missing widened UUID indexes: {string.Join(", ", missingIndexes)}");
    }
}
