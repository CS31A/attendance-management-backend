using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyIntIdColumns : Migration
    {
        private static readonly string[] TargetTables =
        [
            "Admins",
            "Students",
            "Instructors",
            "Courses",
            "Subjects",
            "Sections",
            "Classrooms",
            "Schedules",
            "StudentEnrollments",
            "Sessions",
            "AttendanceRecords",
            "QrCodes",
            "Fingerprints",
            "FingerprintDevices",
            "FingerprintEnrollmentSessions",
            "FingerprintScanEvents",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            // Drop LegacyIntId indexes first
            migrationBuilder.Sql(DropLegacyIntIdIndexesSql());

            // Drop LegacyIntId columns
            migrationBuilder.Sql(DropLegacyIntIdColumnsSql());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            // Restore LegacyIntId columns
            migrationBuilder.Sql(RestoreLegacyIntIdColumnsSql());

            // Restore LegacyIntId indexes
            migrationBuilder.Sql(CreateLegacyIntIdIndexesSql());
        }

        private static bool IsSqlServer(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

        private static string DropLegacyIntIdIndexesSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_LegacyIntId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        DROP INDEX [IX_{table}_LegacyIntId] ON [dbo].[{table}];
                    END;
                    """));

        private static string DropLegacyIntIdColumnsSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF COL_LENGTH(N'dbo.{table}', N'LegacyIntId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[{table}] DROP COLUMN [LegacyIntId];
                    END;
                    """));

        private static string RestoreLegacyIntIdColumnsSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF COL_LENGTH(N'dbo.{table}', N'LegacyIntId') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[{table}] ADD [LegacyIntId] int NULL;
                    END;
                    """));

        private static string CreateLegacyIntIdIndexesSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_LegacyIntId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_{table}_LegacyIntId] ON [dbo].[{table}] ([LegacyIntId]) WHERE [LegacyIntId] IS NOT NULL;
                    END;
                    """));
    }
}
