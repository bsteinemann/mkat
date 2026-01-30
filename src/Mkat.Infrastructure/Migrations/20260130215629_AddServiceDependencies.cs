using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mkat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuppressed",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SuppressionReason",
                table: "Services",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependentServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependencyServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDependencies_Services_DependencyServiceId",
                        column: x => x.DependencyServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDependencies_Services_DependentServiceId",
                        column: x => x.DependentServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDependencies_DependencyServiceId",
                table: "ServiceDependencies",
                column: "DependencyServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDependencies_DependentServiceId_DependencyServiceId",
                table: "ServiceDependencies",
                columns: new[] { "DependentServiceId", "DependencyServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDependencies");

            migrationBuilder.DropColumn(
                name: "IsSuppressed",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "SuppressionReason",
                table: "Services");
        }
    }
}
