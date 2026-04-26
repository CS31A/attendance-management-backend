using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUsnForExistingStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill Usn for existing students with placeholder values
            // Format: PENDING-{full GUID without hyphens}
            migrationBuilder.Sql(@"
                UPDATE Students
                SET Usn = 'PENDING-' + REPLACE(CONVERT(varchar(36), Id), '-', '')
                WHERE Usn IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert backfill by setting Usn to NULL for placeholder values
            migrationBuilder.Sql(@"
                UPDATE Students
                SET Usn = NULL
                WHERE Usn LIKE 'PENDING-%'
            ");
        }
    }
}
