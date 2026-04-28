using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSliceAAcademicUuidColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            AddUuidColumn(migrationBuilder, "Courses");
            AddUuidColumn(migrationBuilder, "Subjects");
            AddUuidColumn(migrationBuilder, "Sections");
            AddUuidColumn(migrationBuilder, "Classrooms");
            AddUuidColumn(migrationBuilder, "Schedules");
            AddUuidColumn(migrationBuilder, "StudentEnrollments");

            BackfillAndHardenUuidColumn(migrationBuilder, "Courses", nullErrorCode: 51010, duplicateErrorCode: 51011, indexName: "IX_Courses_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "Subjects", nullErrorCode: 51012, duplicateErrorCode: 51013, indexName: "IX_Subjects_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "Sections", nullErrorCode: 51014, duplicateErrorCode: 51015, indexName: "IX_Sections_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "Classrooms", nullErrorCode: 51016, duplicateErrorCode: 51017, indexName: "IX_Classrooms_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "Schedules", nullErrorCode: 51018, duplicateErrorCode: 51019, indexName: "IX_Schedules_Uuid");
            BackfillAndHardenUuidColumn(migrationBuilder, "StudentEnrollments", nullErrorCode: 51020, duplicateErrorCode: 51021, indexName: "IX_StudentEnrollments_Uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AddSliceAAcademicUuidColumns is forward-only. Restore from backup before this migration to roll back.");
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
