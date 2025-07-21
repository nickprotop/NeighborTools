using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing unique constraint that doesn't filter nulls properly
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ExternalPaymentId",
                table: "Payments");

            // Recreate the unique constraint with proper null filtering
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ExternalPaymentId",
                table: "Payments",
                columns: new[] { "Provider", "ExternalPaymentId" },
                unique: true,
                filter: "ExternalPaymentId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the filtered constraint
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ExternalPaymentId",
                table: "Payments");

            // Recreate the original constraint without filter (this will recreate the problem)
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ExternalPaymentId",
                table: "Payments",
                columns: new[] { "Provider", "ExternalPaymentId" },
                unique: true);
        }
    }
}
