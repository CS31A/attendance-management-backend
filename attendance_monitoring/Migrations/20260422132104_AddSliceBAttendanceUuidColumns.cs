using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSliceBAttendanceUuidColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            AddUuidColumn(migrationBuilder, "Sessions");
            AddUuidColumn(migrationBuilder, "AttendanceRecords");
            AddUuidColumn(migrationBuilder, "QrCodes");

            BackfillAndHardenUuidColumn(migrationBuilder, "Sessions", nullErrorCode: 51022, duplicateErrorCode: 51023, indexName: "IX_Sessions_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "AttendanceRecords", nullErrorCode: 51024, duplicateErrorCode: 51025, indexName: "IX_AttendanceRecords_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "QrCodes", nullErrorCode: 51026, duplicateErrorCode: 51027, indexName: "IX_QrCodes_Uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AddSliceBAttendanceUuidColumns is forward-only. Restore from backup before this migration to roll back.");
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
