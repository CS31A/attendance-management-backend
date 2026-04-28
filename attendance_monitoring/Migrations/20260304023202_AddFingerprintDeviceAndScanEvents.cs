using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddFingerprintDeviceAndScanEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FingerprintDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FingerprintDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SensorFingerprintId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fingerprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fingerprints_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FingerprintScanEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    MatchedStudentId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    AttendanceRecordId = table.Column<int>(type: "int", nullable: true),
                    MatchScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ThresholdUsed = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FingerprintScanEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FingerprintScanEvents_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FingerprintScanEvents_FingerprintDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "FingerprintDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FingerprintScanEvents_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FingerprintScanEvents_Students_MatchedStudentId",
                        column: x => x.MatchedStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintDevices_DeviceIdentifier",
                table: "FingerprintDevices",
                column: "DeviceIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintDevices_IsActive",
                table: "FingerprintDevices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_DeviceId_SensorFingerprintId",
                table: "Fingerprints",
                columns: new[] { "DeviceId", "SensorFingerprintId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_UserId",
                table: "Fingerprints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_AttendanceRecordId",
                table: "FingerprintScanEvents",
                column: "AttendanceRecordId",
                unique: true,
                filter: "[AttendanceRecordId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_DeviceId_CapturedAt",
                table: "FingerprintScanEvents",
                columns: new[] { "DeviceId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_EventId",
                table: "FingerprintScanEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_MatchedStudentId_CapturedAt",
                table: "FingerprintScanEvents",
                columns: new[] { "MatchedStudentId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_SessionId",
                table: "FingerprintScanEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintScanEvents_Status",
                table: "FingerprintScanEvents",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fingerprints");

            migrationBuilder.DropTable(
                name: "FingerprintScanEvents");

            migrationBuilder.DropTable(
                name: "FingerprintDevices");
        }
    }
}
