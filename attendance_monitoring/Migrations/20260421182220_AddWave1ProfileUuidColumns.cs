using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddWave1ProfileUuidColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: "Students",
                type: "uniqueidentifier",
                nullable: true,
                defaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: "Instructors",
                type: "uniqueidentifier",
                nullable: true,
                defaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: "Admins",
                type: "uniqueidentifier",
                nullable: true,
                defaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.Sql(@"
                UPDATE [Students]
                SET [Uuid] = DEFAULT
                WHERE [Uuid] IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM [Students]
                    WHERE [Uuid] IS NULL
                )
                BEGIN
                    THROW 51000, 'Students contains NULL Uuid values after backfill.', 1;
                END;

                IF EXISTS (
                    SELECT [Uuid]
                    FROM [Students]
                    GROUP BY [Uuid]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51001, 'Students contains duplicate Uuid values after backfill.', 1;
                END;
            ");

            migrationBuilder.Sql(@"
                UPDATE [Instructors]
                SET [Uuid] = DEFAULT
                WHERE [Uuid] IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM [Instructors]
                    WHERE [Uuid] IS NULL
                )
                BEGIN
                    THROW 51002, 'Instructors contains NULL Uuid values after backfill.', 1;
                END;

                IF EXISTS (
                    SELECT [Uuid]
                    FROM [Instructors]
                    GROUP BY [Uuid]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51003, 'Instructors contains duplicate Uuid values after backfill.', 1;
                END;
            ");

            migrationBuilder.Sql(@"
                UPDATE [Admins]
                SET [Uuid] = DEFAULT
                WHERE [Uuid] IS NULL;

                IF EXISTS (
                    SELECT 1
                    FROM [Admins]
                    WHERE [Uuid] IS NULL
                )
                BEGIN
                    THROW 51004, 'Admins contains NULL Uuid values after backfill.', 1;
                END;

                IF EXISTS (
                    SELECT [Uuid]
                    FROM [Admins]
                    GROUP BY [Uuid]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51005, 'Admins contains duplicate Uuid values after backfill.', 1;
                END;
            ");

            migrationBuilder.Sql("ALTER TABLE [Students] ALTER COLUMN [Uuid] uniqueidentifier NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [Instructors] ALTER COLUMN [Uuid] uniqueidentifier NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [Admins] ALTER COLUMN [Uuid] uniqueidentifier NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Uuid",
                table: "Students",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_Uuid",
                table: "Instructors",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Uuid",
                table: "Admins",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AddWave1ProfileUuidColumns is forward-only. Restore from backup before this migration to roll back.");
        }
    }
}
