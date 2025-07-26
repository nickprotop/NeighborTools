using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFraudCheckPaymentIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FraudChecks_AspNetUsers_UserId",
                table: "FraudChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_FraudChecks_Payments_PaymentId1",
                table: "FraudChecks");

            migrationBuilder.DropIndex(
                name: "IX_FraudChecks_PaymentId1",
                table: "FraudChecks");

            migrationBuilder.DropColumn(
                name: "PaymentId1",
                table: "FraudChecks");

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskScore",
                table: "FraudChecks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentId",
                table: "FraudChecks",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CheckDetails",
                table: "FraudChecks",
                type: "JSON",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_PaymentId",
                table: "FraudChecks",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_RiskLevel",
                table: "FraudChecks",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_Status",
                table: "FraudChecks",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_FraudChecks_AspNetUsers_UserId",
                table: "FraudChecks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraudChecks_Payments_PaymentId",
                table: "FraudChecks",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FraudChecks_AspNetUsers_UserId",
                table: "FraudChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_FraudChecks_Payments_PaymentId",
                table: "FraudChecks");

            migrationBuilder.DropIndex(
                name: "IX_FraudChecks_PaymentId",
                table: "FraudChecks");

            migrationBuilder.DropIndex(
                name: "IX_FraudChecks_RiskLevel",
                table: "FraudChecks");

            migrationBuilder.DropIndex(
                name: "IX_FraudChecks_Status",
                table: "FraudChecks");

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskScore",
                table: "FraudChecks",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentId",
                table: "FraudChecks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "CheckDetails",
                table: "FraudChecks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "JSON")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId1",
                table: "FraudChecks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_PaymentId1",
                table: "FraudChecks",
                column: "PaymentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_FraudChecks_AspNetUsers_UserId",
                table: "FraudChecks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FraudChecks_Payments_PaymentId1",
                table: "FraudChecks",
                column: "PaymentId1",
                principalTable: "Payments",
                principalColumn: "Id");
        }
    }
}
