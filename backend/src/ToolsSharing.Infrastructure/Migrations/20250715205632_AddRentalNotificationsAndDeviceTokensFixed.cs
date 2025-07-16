using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalNotificationsAndDeviceTokensFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RentalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NotificationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RecipientId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalNotifications_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalNotifications_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceToken = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Platform = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    LastUsed = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeviceInfo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotifications_RecipientId",
                table: "RentalNotifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotifications_RentalId",
                table: "RentalNotifications",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotifications_RentalId_NotificationType",
                table: "RentalNotifications",
                columns: new[] { "RentalId", "NotificationType" });

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotifications_SentAt",
                table: "RentalNotifications",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_DeviceToken",
                table: "UserDeviceTokens",
                column: "DeviceToken");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId_IsActive",
                table: "UserDeviceTokens",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalNotifications");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");
        }
    }
}
