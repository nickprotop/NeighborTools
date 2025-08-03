using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConversationUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations",
                columns: new[] { "Participant1Id", "Participant2Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations",
                columns: new[] { "Participant1Id", "Participant2Id" },
                unique: true);
        }
    }
}
