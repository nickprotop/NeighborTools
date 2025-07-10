using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGDPRTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserConsents table
            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ConsentType = table.Column<string>(type: "enum('cookies','marketing','analytics','data_processing','financial_data','location_data')", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ConsentSource = table.Column<string>(type: "varchar(100)", nullable: false),
                    ConsentVersion = table.Column<string>(type: "varchar(20)", nullable: false),
                    IPAddress = table.Column<string>(type: "varchar(45)", nullable: false),
                    UserAgent = table.Column<string>(type: "varchar(500)", nullable: true),
                    WithdrawnDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "varchar(500)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create DataProcessingLog table
            migrationBuilder.CreateTable(
                name: "DataProcessingLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<string>(type: "varchar(100)", nullable: false),
                    DataCategories = table.Column<string>(type: "json", nullable: false),
                    ProcessingPurpose = table.Column<string>(type: "varchar(500)", nullable: false),
                    LegalBasis = table.Column<string>(type: "enum('consent','contract','legal_obligation','vital_interests','public_task','legitimate_interests')", nullable: false),
                    DataSources = table.Column<string>(type: "json", nullable: false),
                    DataRecipients = table.Column<string>(type: "json", nullable: true),
                    RetentionPeriod = table.Column<string>(type: "varchar(100)", nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IPAddress = table.Column<string>(type: "varchar(45)", nullable: false),
                    UserAgent = table.Column<string>(type: "varchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProcessingLog_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create DataSubjectRequests table
            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<string>(type: "enum('access','rectification','erasure','portability','restriction','objection')", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RequestDetails = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "enum('pending','in_progress','completed','rejected','partially_completed')", nullable: false, defaultValue: "pending"),
                    ResponseDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ResponseDetails = table.Column<string>(type: "text", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProcessedByUserId = table.Column<int>(type: "int", nullable: true),
                    DataExportPath = table.Column<string>(type: "varchar(500)", nullable: true),
                    VerificationMethod = table.Column<string>(type: "varchar(100)", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create PrivacyPolicyVersions table
            migrationBuilder.CreateTable(
                name: "PrivacyPolicyVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    Version = table.Column<string>(type: "varchar(20)", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyPolicyVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyPolicyVersions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            // Create remaining tables...
            migrationBuilder.CreateTable(
                name: "CookieConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CookieCategory = table.Column<string>(type: "enum('essential','functional','analytics','marketing')", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    IPAddress = table.Column<string>(type: "varchar(45)", nullable: false),
                    UserAgent = table.Column<string>(type: "varchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CookieConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add columns to existing Users table
            migrationBuilder.AddColumn<bool>(
                name: "DataProcessingConsent",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRetentionDate",
                table: "Users",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConsentUpdate",
                table: "Users",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GDPROptOut",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DataPortabilityRequested",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizationDate",
                table: "Users",
                type: "datetime",
                nullable: true);

            // Add columns to existing FinancialAuditLog table
            migrationBuilder.AddColumn<string>(
                name: "DataCategories",
                table: "FinancialAuditLog",
                type: "json",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalBasis",
                table: "FinancialAuditLog",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsentReference",
                table: "FinancialAuditLog",
                type: "int",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "idx_user_consent_type",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentType" });

            migrationBuilder.CreateIndex(
                name: "idx_consent_date",
                table: "UserConsents",
                column: "ConsentDate");

            migrationBuilder.CreateIndex(
                name: "idx_user_activity",
                table: "DataProcessingLog",
                columns: new[] { "UserId", "ActivityType" });

            migrationBuilder.CreateIndex(
                name: "idx_request_status",
                table: "DataSubjectRequests",
                column: "Status");

            // Insert default data
            migrationBuilder.InsertData(
                table: "PrivacyPolicyVersions",
                columns: new[] { "Version", "Content", "EffectiveDate", "CreatedBy", "IsActive" },
                values: new object[] { "1.0", "GDPR-compliant privacy policy content", DateTime.UtcNow, 1, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "fk_consent_reference",
                table: "FinancialAuditLog");

            // Drop added columns from existing tables
            migrationBuilder.DropColumn(name: "DataProcessingConsent", table: "Users");
            migrationBuilder.DropColumn(name: "MarketingConsent", table: "Users");
            migrationBuilder.DropColumn(name: "DataRetentionDate", table: "Users");
            migrationBuilder.DropColumn(name: "LastConsentUpdate", table: "Users");
            migrationBuilder.DropColumn(name: "GDPROptOut", table: "Users");
            migrationBuilder.DropColumn(name: "DataPortabilityRequested", table: "Users");
            migrationBuilder.DropColumn(name: "AnonymizationDate", table: "Users");

            migrationBuilder.DropColumn(name: "DataCategories", table: "FinancialAuditLog");
            migrationBuilder.DropColumn(name: "LegalBasis", table: "FinancialAuditLog");
            migrationBuilder.DropColumn(name: "ConsentReference", table: "FinancialAuditLog");

            // Drop GDPR tables
            migrationBuilder.DropTable(name: "UserConsents");
            migrationBuilder.DropTable(name: "DataProcessingLog");
            migrationBuilder.DropTable(name: "DataSubjectRequests");
            migrationBuilder.DropTable(name: "PrivacyPolicyVersions");
            migrationBuilder.DropTable(name: "CookieConsents");
        }
    }
}