using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mkat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthCheckFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyMatchRegex",
                table: "Monitors",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedStatusCodes",
                table: "Monitors",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthCheckUrl",
                table: "Monitors",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "Monitors",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "Monitors",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyMatchRegex",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "ExpectedStatusCodes",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "HealthCheckUrl",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "Monitors");
        }
    }
}
