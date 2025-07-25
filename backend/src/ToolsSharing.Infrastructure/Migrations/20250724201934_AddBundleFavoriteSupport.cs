using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleFavoriteSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_ToolId",
                table: "Favorites");

            migrationBuilder.AlterColumn<Guid>(
                name: "ToolId",
                table: "Favorites",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "BundleId",
                table: "Favorites",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "FavoriteType",
                table: "Favorites",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_BundleId",
                table: "Favorites",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_FavoriteType",
                table: "Favorites",
                column: "FavoriteType");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_BundleId",
                table: "Favorites",
                columns: new[] { "UserId", "BundleId" },
                unique: true,
                filter: "[BundleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_ToolId",
                table: "Favorites",
                columns: new[] { "UserId", "ToolId" },
                unique: true,
                filter: "[ToolId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Bundles_BundleId",
                table: "Favorites",
                column: "BundleId",
                principalTable: "Bundles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Bundles_BundleId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_BundleId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_FavoriteType",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_BundleId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_ToolId",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "BundleId",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "FavoriteType",
                table: "Favorites");

            migrationBuilder.AlterColumn<Guid>(
                name: "ToolId",
                table: "Favorites",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_ToolId",
                table: "Favorites",
                columns: new[] { "UserId", "ToolId" },
                unique: true);
        }
    }
}
