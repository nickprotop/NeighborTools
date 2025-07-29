using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePublicLocationToLocationDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublicLocation",
                table: "AspNetUsers",
                newName: "LocationDisplay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationDisplay",
                table: "AspNetUsers",
                newName: "PublicLocation");
        }
    }
}
