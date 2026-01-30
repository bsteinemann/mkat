using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mkat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMetricReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetricReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonitorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsOutOfRange = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false)
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
    }
}
