using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalFieldsToToolsAndBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Tools",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Tools",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Tools",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PendingApproval",
                table: "Tools",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Tools",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Bundles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Bundles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Bundles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PendingApproval",
                table: "Bundles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Bundles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "PendingApproval",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "PendingApproval",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Bundles");
        }
    }
}
