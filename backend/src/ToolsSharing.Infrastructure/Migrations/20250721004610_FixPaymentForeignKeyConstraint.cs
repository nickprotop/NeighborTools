using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentForeignKeyConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the incorrect foreign key constraint that references Transactions table
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Transactions_RentalId",
                table: "Payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the foreign key constraint for rollback (though this will recreate the problematic constraint)
            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Transactions_RentalId",
                table: "Payments",
                column: "RentalId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
