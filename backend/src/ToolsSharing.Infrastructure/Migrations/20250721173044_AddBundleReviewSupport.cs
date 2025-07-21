using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleReviewSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BundleRentalId",
                table: "Transactions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ExternalTransactionId",
                table: "Transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProviderId",
                table: "Transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "BundleId",
                table: "Reviews",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "BundleRentalId",
                table: "Reviews",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BundleRentalId",
                table: "Transactions",
                column: "BundleRentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BundleId",
                table: "Reviews",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BundleRentalId",
                table: "Reviews",
                column: "BundleRentalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_BundleRentals_BundleRentalId",
                table: "Reviews",
                column: "BundleRentalId",
                principalTable: "BundleRentals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Bundles_BundleId",
                table: "Reviews",
                column: "BundleId",
                principalTable: "Bundles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BundleRentals_BundleRentalId",
                table: "Transactions",
                column: "BundleRentalId",
                principalTable: "BundleRentals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_BundleRentals_BundleRentalId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Bundles_BundleId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BundleRentals_BundleRentalId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BundleRentalId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BundleId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BundleRentalId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "BundleRentalId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentProviderId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BundleId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "BundleRentalId",
                table: "Reviews");
        }
    }
}
