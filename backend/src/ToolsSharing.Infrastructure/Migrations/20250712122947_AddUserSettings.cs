using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowProfilePicture = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowRealName = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowLocation = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowPhoneNumber = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowEmail = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowStatistics = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EmailRentalRequests = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EmailRentalUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EmailMessages = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EmailMarketing = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EmailSecurityAlerts = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PushMessages = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PushReminders = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PushRentalRequests = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PushRentalUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Theme = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "system")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "en")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "USD")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeZone = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutoApproveRentals = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RentalLeadTime = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    RequireDeposit = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DefaultDepositPercentage = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 0.20m),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    LoginAlertsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 480),
                    AllowDirectMessages = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    AllowRentalInquiries = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowOnlineStatus = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
