using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class CreateQrCodesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualRoomId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsage = table.Column<int>(type: "int", nullable: true),
                    QrHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrCodes_Classrooms_ActualRoomId",
                        column: x => x.ActualRoomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrCodes_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrCodes_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_ActualRoomId",
                table: "QrCodes",
                column: "ActualRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_ExpiresAt",
                table: "QrCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_IsActive",
                table: "QrCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_QrHash",
                table: "QrCodes",
                column: "QrHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_ScheduleId",
                table: "QrCodes",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_SectionId",
                table: "QrCodes",
                column: "SectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrCodes");
        }
    }
}
