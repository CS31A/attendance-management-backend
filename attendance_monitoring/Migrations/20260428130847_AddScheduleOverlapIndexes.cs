using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleOverlapIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_InstructorId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SectionId",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_InstructorId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules",
                columns: new[] { "InstructorId", "DayOfWeek", "TimeIn", "TimeOut" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SectionId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules",
                columns: new[] { "SectionId", "DayOfWeek", "TimeIn", "TimeOut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_InstructorId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SectionId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_InstructorId",
                table: "Schedules",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SectionId",
                table: "Schedules",
                column: "SectionId");
        }
    }
}
