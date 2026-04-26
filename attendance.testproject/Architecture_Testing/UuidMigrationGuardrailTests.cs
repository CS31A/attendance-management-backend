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

    private static readonly string[] GuidPrimaryKeyEntityNames =
    [
        "Admin",
        "AttendanceRecord",
        "Classroom",
        "Course",
        "Fingerprint",
        "FingerprintDevice",
        "FingerprintEnrollmentSession",
        "FingerprintScanEvent",
        "Instructor",
        "QrCode",
        "Schedules",
        "Section",
        "Session",
        "Student",
        "StudentEnrollment",
        "Subject"
    ];

    private static readonly Dictionary<string, string> GuidPrimaryKeyEntityFiles = new()
    {
        ["Admin"] = "Admin.cs",
        ["AttendanceRecord"] = "AttendanceRecord.cs",
        ["Classroom"] = "Classroom.cs",
        ["Course"] = "Course.cs",
        ["Fingerprint"] = "Fingerprint.cs",
        ["FingerprintDevice"] = "FingerprintDevice.cs",
        ["FingerprintEnrollmentSession"] = "FingerprintEnrollmentSession.cs",
        ["FingerprintScanEvent"] = "FingerprintScanEvent.cs",
        ["Instructor"] = "Instructor.cs",
        ["QrCode"] = "QrCode.cs",
        ["Schedules"] = "Schedule.cs",
        ["Section"] = "Section.cs",
        ["Session"] = "Session.cs",
        ["Student"] = "Student.cs",
        ["StudentEnrollment"] = "StudentEnrollment.cs",
        ["Subject"] = "Subject.cs"
    };

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
    public void ApplicationDbContextModelSnapshot_ShouldUseGuidIdsAndNoUuidProperties()
    {
        var snapshotRelativePath = Path.Combine("attendance_monitoring", "Migrations", "ApplicationDbContextModelSnapshot.cs");
        var snapshotPath = Path.Combine(RepositoryRoot, snapshotRelativePath);
        var snapshotContent = File.ReadAllText(snapshotPath);

        var entityBlocks = ExtractSnapshotEntityBlocks(snapshotContent);
        var offenders = new List<string>();

        foreach (var entityName in GuidPrimaryKeyEntityNames)
        {
            if (!entityBlocks.TryGetValue(entityName, out var entityBlock))
            {
                offenders.Add($"{entityName} missing from model snapshot");
                continue;
            }

            if (!entityBlock.Contains("b.Property<Guid>(\"Id\")", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} does not declare Guid Id");
            }

            if (entityBlock.Contains("b.Property<int>(\"Id\")", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} still declares int Id");
            }

            if (entityBlock.Contains("b.Property<Guid>(\"Uuid\")", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} still declares Uuid");
            }

            if (!entityBlock.Contains("b.HasKey(\"Id\")", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} does not use Id as key");
            }
        }

        Assert.True(offenders.Count == 0,
            $"Model snapshot is not Guid-primary-key-only: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void TargetEntitySources_ShouldUseGuidIdsAndNoUuidProperties()
    {
        var offenders = new List<string>();

        foreach (var (entityName, fileName) in GuidPrimaryKeyEntityFiles)
        {
            var path = Path.Combine(RepositoryRoot, "attendance_monitoring", "Classes", fileName);
            var content = File.ReadAllText(path);

            if (!content.Contains("public Guid Id { get; set; }", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} source does not declare Guid Id");
            }

            if (content.Contains("public int Id { get; set; }", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} source still declares int Id");
            }

            if (content.Contains("public Guid Uuid { get; set; }", StringComparison.Ordinal))
            {
                offenders.Add($"{entityName} source still declares Uuid");
            }
        }

        Assert.True(offenders.Count == 0,
            $"Target entity sources are not Guid-primary-key-only: {string.Join(", ", offenders)}");
    }

    private static Dictionary<string, string> ExtractSnapshotEntityBlocks(string snapshotContent)
    {
        var blocks = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entityName in GuidPrimaryKeyEntityNames)
        {
            var marker = $"modelBuilder.Entity(\"attendance_monitoring.Classes.{entityName}\"";
            var start = snapshotContent.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            var nextEntity = snapshotContent.IndexOf("\n            modelBuilder.Entity(", start + marker.Length, StringComparison.Ordinal);
            var end = nextEntity >= 0 ? nextEntity : snapshotContent.Length;
            blocks[entityName] = snapshotContent[start..end];
        }

        return blocks;
    }
}
