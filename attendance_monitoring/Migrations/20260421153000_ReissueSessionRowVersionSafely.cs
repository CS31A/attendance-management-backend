using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <summary>
    /// Preserves the original 20260419154812 migration for databases that already applied it,
    /// while reissuing the SQL Server rowversion in a new migration for all environments.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260421153000_ReissueSessionRowVersionSafely")]
    public partial class ReissueSessionRowVersionSafely : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            new SnapshotProxy().Populate(modelBuilder);
        }

        private static bool IsSqlServer(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

        private sealed class SnapshotProxy : ApplicationDbContextModelSnapshot
        {
            public void Populate(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
        }
    }
}
