using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsOfServiceAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TermsOfServiceAccepted",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsAcceptedDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsOfServiceAccepted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "AspNetUsers");
        }
    }
}
