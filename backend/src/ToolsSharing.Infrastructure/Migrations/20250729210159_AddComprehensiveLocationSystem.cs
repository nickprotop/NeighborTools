using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprehensiveLocationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationArea",
                table: "Tools",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCity",
                table: "Tools",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCountry",
                table: "Tools",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationDisplay",
                table: "Tools",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLat",
                table: "Tools",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLng",
                table: "Tools",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrecisionRadius",
                table: "Tools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrivacyLevel",
                table: "Tools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocationSource",
                table: "Tools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationState",
                table: "Tools",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAt",
                table: "Tools",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationArea",
                table: "Bundles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCity",
                table: "Bundles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCountry",
                table: "Bundles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationDisplay",
                table: "Bundles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLat",
                table: "Bundles",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLng",
                table: "Bundles",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrecisionRadius",
                table: "Bundles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrivacyLevel",
                table: "Bundles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocationSource",
                table: "Bundles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationState",
                table: "Bundles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAt",
                table: "Bundles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LocationDisplay",
                table: "AspNetUsers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationArea",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCity",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCountry",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLat",
                table: "AspNetUsers",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLng",
                table: "AspNetUsers",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrecisionRadius",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationPrivacyLevel",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocationSource",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationState",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAt",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocationSearchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SearchType = table.Column<int>(type: "int", nullable: false),
                    SearchLat = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    SearchLng = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    SearchRadiusKm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SearchQuery = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuspicious = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SuspiciousReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationSearchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationSearchLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_LocationArea",
                table: "Tools",
                column: "LocationArea");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_LocationAvailability",
                table: "Tools",
                columns: new[] { "LocationLat", "LocationLng", "IsAvailable", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Tools_LocationCity",
                table: "Tools",
                column: "LocationCity");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_LocationCityState",
                table: "Tools",
                columns: new[] { "LocationCity", "LocationState" });

            migrationBuilder.CreateIndex(
                name: "IX_Tools_LocationCoordinates",
                table: "Tools",
                columns: new[] { "LocationLat", "LocationLng" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_LocationArea",
                table: "Bundles",
                column: "LocationArea");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_LocationAvailability",
                table: "Bundles",
                columns: new[] { "LocationLat", "LocationLng", "IsPublished", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_LocationCity",
                table: "Bundles",
                column: "LocationCity");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_LocationCityState",
                table: "Bundles",
                columns: new[] { "LocationCity", "LocationState" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_LocationCoordinates",
                table: "Bundles",
                columns: new[] { "LocationLat", "LocationLng" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocationArea",
                table: "AspNetUsers",
                column: "LocationArea");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocationCity",
                table: "AspNetUsers",
                column: "LocationCity");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocationCityState",
                table: "AspNetUsers",
                columns: new[] { "LocationCity", "LocationState" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocationCoordinates",
                table: "AspNetUsers",
                columns: new[] { "LocationLat", "LocationLng" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_CreatedAt",
                table: "LocationSearchLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_IpAddress",
                table: "LocationSearchLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_IpTargetTime",
                table: "LocationSearchLogs",
                columns: new[] { "IpAddress", "TargetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_IsSuspicious",
                table: "LocationSearchLogs",
                column: "IsSuspicious");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_LocationTime",
                table: "LocationSearchLogs",
                columns: new[] { "SearchLat", "SearchLng", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_SessionId",
                table: "LocationSearchLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_SessionTime",
                table: "LocationSearchLogs",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_TargetId",
                table: "LocationSearchLogs",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_UserId",
                table: "LocationSearchLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationSearchLog_UserTargetTime",
                table: "LocationSearchLogs",
                columns: new[] { "UserId", "TargetId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationSearchLogs");

            migrationBuilder.DropIndex(
                name: "IX_Tools_LocationArea",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Tools_LocationAvailability",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Tools_LocationCity",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Tools_LocationCityState",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Tools_LocationCoordinates",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_LocationArea",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_LocationAvailability",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_LocationCity",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_LocationCityState",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_LocationCoordinates",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Users_LocationArea",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Users_LocationCity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Users_LocationCityState",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Users_LocationCoordinates",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationArea",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationCity",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationCountry",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationDisplay",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationPrecisionRadius",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationPrivacyLevel",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationSource",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationState",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAt",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "LocationArea",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationCity",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationCountry",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationDisplay",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationPrecisionRadius",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationPrivacyLevel",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationSource",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationState",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAt",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LocationArea",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationCity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationCountry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationPrecisionRadius",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationPrivacyLevel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationSource",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationState",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "LocationDisplay",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
