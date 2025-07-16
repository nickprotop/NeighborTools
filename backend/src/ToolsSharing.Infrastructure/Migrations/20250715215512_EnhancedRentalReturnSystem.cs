using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedRentalReturnSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeDeadline",
                table: "Rentals",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnConditionNotes",
                table: "Rentals",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnedByUserId",
                table: "Rentals",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeDeadline",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "ReturnConditionNotes",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "ReturnedByUserId",
                table: "Rentals");
        }
    }
}
