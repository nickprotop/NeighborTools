using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixExistingFavoriteTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing favorites to set proper FavoriteType
            // All existing favorites with ToolId should be type "Tool"
            migrationBuilder.Sql(@"
                UPDATE Favorites 
                SET FavoriteType = 'Tool' 
                WHERE ToolId IS NOT NULL AND (FavoriteType = '' OR FavoriteType IS NULL)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
