using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mkat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricReadingAndMonitorMetricFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMetricAt",
                table: "Monitors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastMetricValue",
                table: "Monitors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxValue",
                table: "Monitors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinValue",
                table: "Monitors",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "Monitors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThresholdCount",
                table: "Monitors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThresholdStrategy",
                table: "Monitors",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WindowSampleCount",
                table: "Monitors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WindowSeconds",
                table: "Monitors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MetricReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonitorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsOutOfRange = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricReadings_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricReadings_MonitorId_RecordedAt",
                table: "MetricReadings",
                columns: new[] { "MonitorId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricReadings");

            migrationBuilder.DropColumn(
                name: "LastMetricAt",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "LastMetricValue",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "ThresholdCount",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "ThresholdStrategy",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "WindowSampleCount",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "WindowSeconds",
                table: "Monitors");
        }
    }
}
