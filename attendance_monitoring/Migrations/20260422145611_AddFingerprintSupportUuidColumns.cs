using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddFingerprintSupportUuidColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            AddUuidColumn(migrationBuilder, "Fingerprints");
            AddUuidColumn(migrationBuilder, "FingerprintDevices");
            AddUuidColumn(migrationBuilder, "FingerprintEnrollmentSessions");
            AddUuidColumn(migrationBuilder, "FingerprintScanEvents");

            BackfillAndHardenUuidColumn(migrationBuilder, "Fingerprints", nullErrorCode: 51028, duplicateErrorCode: 51029, indexName: "IX_Fingerprints_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "FingerprintDevices", nullErrorCode: 51030, duplicateErrorCode: 51031, indexName: "IX_FingerprintDevices_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "FingerprintEnrollmentSessions", nullErrorCode: 51032, duplicateErrorCode: 51033, indexName: "IX_FingerprintEnrollmentSessions_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "FingerprintScanEvents", nullErrorCode: 51034, duplicateErrorCode: 51035, indexName: "IX_FingerprintScanEvents_Uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AddFingerprintSupportUuidColumns is forward-only. Restore from backup before this migration to roll back.");
        }

        private static void AddUuidColumn(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: tableName,
                type: "uniqueidentifier",
                nullable: true,
                defaultValueSql: "NEWSEQUENTIALID()");
        }

        private static void BackfillAndHardenUuidColumn(
            MigrationBuilder migrationBuilder,
            string tableName,
            int nullErrorCode,
            int duplicateErrorCode,
            string indexName)
        {
            migrationBuilder.Sql($@"
                UPDATE [{tableName}]
                SET [Uuid] = DEFAULT
                WHERE [Uuid] IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM [{tableName}]
                    WHERE [Uuid] IS NULL
                )
                BEGIN
                    THROW {nullErrorCode}, '{tableName} contains NULL Uuid values after backfill.', 1;
                END;

                IF EXISTS (
                    SELECT [Uuid]
                    FROM [{tableName}]
                    GROUP BY [Uuid]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW {duplicateErrorCode}, '{tableName} contains duplicate Uuid values after backfill.', 1;
                END;
            ");

            migrationBuilder.Sql($"ALTER TABLE [{tableName}] ALTER COLUMN [Uuid] uniqueidentifier NOT NULL;");

            migrationBuilder.CreateIndex(
                name: indexName,
                table: tableName,
                column: "Uuid",
                unique: true);
        }

        private static bool IsSqlServer(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
    }
}
