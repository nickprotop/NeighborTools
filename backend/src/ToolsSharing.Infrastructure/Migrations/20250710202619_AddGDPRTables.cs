using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGDPRTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizationDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DataPortabilityRequested",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DataProcessingConsent",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRetentionDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GDPROptOut",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConsentUpdate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CookieConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CookieCategory = table.Column<int>(type: "int", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CookieConsents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActivityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCategories = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessingPurpose = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LegalBasis = table.Column<int>(type: "int", nullable: false),
                    DataSources = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataRecipients = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetentionPeriod = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProcessingLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RequestDetails = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResponseDetails = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletionDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataExportPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VerificationMethod = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectionReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PrivacyPolicyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyPolicyVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyPolicyVersions_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentType = table.Column<int>(type: "int", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConsentSource = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WithdrawnDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CookieConsents_ExpiryDate",
                table: "CookieConsents",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_CookieConsents_Session_Category",
                table: "CookieConsents",
                columns: new[] { "SessionId", "CookieCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_CookieConsents_User_Category",
                table: "CookieConsents",
                columns: new[] { "UserId", "CookieCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingLogs_ActivityType",
                table: "DataProcessingLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingLogs_User_Date",
                table: "DataProcessingLogs",
                columns: new[] { "UserId", "ProcessingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_ProcessedByUserId",
                table: "DataSubjectRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Status",
                table: "DataSubjectRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_User_Date",
                table: "DataSubjectRequests",
                columns: new[] { "UserId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyPolicyVersions_CreatedBy",
                table: "PrivacyPolicyVersions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyPolicyVersions_EffectiveDate",
                table: "PrivacyPolicyVersions",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyPolicyVersions_IsActive",
                table: "PrivacyPolicyVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyPolicyVersions_Version",
                table: "PrivacyPolicyVersions",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_User_Type_Date",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentType", "ConsentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CookieConsents");

            migrationBuilder.DropTable(
                name: "DataProcessingLogs");

            migrationBuilder.DropTable(
                name: "DataSubjectRequests");

            migrationBuilder.DropTable(
                name: "PrivacyPolicyVersions");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropColumn(
                name: "AnonymizationDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataPortabilityRequested",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataProcessingConsent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataRetentionDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GDPROptOut",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastConsentUpdate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "AspNetUsers");
        }
    }
}
