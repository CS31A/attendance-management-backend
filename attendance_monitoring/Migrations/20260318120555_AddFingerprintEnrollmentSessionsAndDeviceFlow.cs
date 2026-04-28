using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddFingerprintEnrollmentSessionsAndDeviceFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fingerprints_DeviceId_SensorFingerprintId",
                table: "Fingerprints");

            migrationBuilder.DropIndex(
                name: "IX_Fingerprints_UserId",
                table: "Fingerprints");

            migrationBuilder.CreateTable(
                name: "FingerprintEnrollmentSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedSensorFingerprintId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FingerprintEnrollmentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FingerprintEnrollmentSessions_FingerprintDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "FingerprintDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FingerprintEnrollmentSessions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_DeviceId_SensorFingerprintId_Active",
                table: "Fingerprints",
                columns: new[] { "DeviceId", "SensorFingerprintId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_UserId_Active",
                table: "Fingerprints",
                column: "UserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintEnrollmentSessions_DeviceId_Status",
                table: "FingerprintEnrollmentSessions",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintEnrollmentSessions_EnrollmentSessionId",
                table: "FingerprintEnrollmentSessions",
                column: "EnrollmentSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintEnrollmentSessions_StudentId_Status",
                table: "FingerprintEnrollmentSessions",
                columns: new[] { "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FingerprintEnrollmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_Fingerprints_DeviceId_SensorFingerprintId_Active",
                table: "Fingerprints");

            migrationBuilder.DropIndex(
                name: "IX_Fingerprints_UserId_Active",
                table: "Fingerprints");

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_DeviceId_SensorFingerprintId",
                table: "Fingerprints",
                columns: new[] { "DeviceId", "SensorFingerprintId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_UserId",
                table: "Fingerprints",
                column: "UserId");
        }
    }
}
