using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mkat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitorEventsAndRollups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonitorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: true),
                    IsOutOfRange = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitorEvents_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonitorEvents_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonitorRollups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonitorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Granularity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Min = table.Column<double>(type: "REAL", nullable: true),
                    Max = table.Column<double>(type: "REAL", nullable: true),
                    Mean = table.Column<double>(type: "REAL", nullable: true),
                    Median = table.Column<double>(type: "REAL", nullable: true),
                    P80 = table.Column<double>(type: "REAL", nullable: true),
                    P90 = table.Column<double>(type: "REAL", nullable: true),
                    P95 = table.Column<double>(type: "REAL", nullable: true),
                    StdDev = table.Column<double>(type: "REAL", nullable: true),
                    UptimePercent = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorRollups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitorRollups_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonitorRollups_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitorEvents_MonitorId_CreatedAt",
                table: "MonitorEvents",
                columns: new[] { "MonitorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitorEvents_ServiceId_CreatedAt",
                table: "MonitorEvents",
                columns: new[] { "ServiceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitorRollups_MonitorId_Granularity_PeriodStart",
                table: "MonitorRollups",
                columns: new[] { "MonitorId", "Granularity", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitorRollups_ServiceId_Granularity_PeriodStart",
                table: "MonitorRollups",
                columns: new[] { "ServiceId", "Granularity", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitorEvents");

            migrationBuilder.DropTable(
                name: "MonitorRollups");
        }
    }
}
