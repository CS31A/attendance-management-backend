using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <summary>
    /// Drops the sp_UpdateUser stored procedure as part of removing the legacy
    /// stored procedure code path. The EF Core path is now the canonical approach
    /// for user updates per ADR: User Management Persistence Strategy.
    /// </summary>
    public partial class DropUpdateUserStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Recreating the stored procedure in Down would require the full
            // CREATE PROCEDURE script. However, since this is removing a legacy,
            // non-canonical code path (per ADR), restoring it is not recommended.
            // If needed for rollback scenarios, the original migration
            // 20251126040001_AddUpdateUserSP contains the full definition.
        }
    }
}
