using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmailConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, remove any duplicate emails if they exist, keeping only the first occurrence
            // This is essential before applying unique constraint to avoid migration failures
            migrationBuilder.Sql(@"
                WITH DuplicateEmails AS (
                    SELECT Id, NormalizedEmail, 
                           ROW_NUMBER() OVER (PARTITION BY NormalizedEmail ORDER BY Id) AS RowNum
                    FROM AspNetUsers 
                    WHERE NormalizedEmail IS NOT NULL
                )
                DELETE FROM AspNetUsers 
                WHERE Id IN (
                    SELECT Id FROM DuplicateEmails WHERE RowNum > 1
                )");

            // Drop the existing non-unique EmailIndex if it exists
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            // Create unique index on NormalizedEmail column
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedEmail",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedEmail",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
