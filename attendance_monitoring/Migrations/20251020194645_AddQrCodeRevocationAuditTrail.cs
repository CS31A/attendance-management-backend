using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeRevocationAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "QrCodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "QrCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy",
                table: "QrCodes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "QrCodes");
        }
    }
}
