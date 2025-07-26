using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3SecurityEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttackPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AttackType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIdentifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetIdentifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttackData = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstDetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastDetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolutionNotes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BlockDuration = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GeographicData = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgentPatterns = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SuccessfulAttempts = table.Column<int>(type: "int", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackPatterns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlacklistedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TokenId = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlacklistedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AdditionalData = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlacklistedTokens_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BlacklistedTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeographicLocation = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceFingerprint = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ResponseAction = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AdditionalData = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceFingerprint = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeographicLocation = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TerminationReason = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TerminatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeviceName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Platform = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Browser = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuspicious = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ActivityCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_AttackType",
                table: "AttackPatterns",
                column: "AttackType");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_AttackType_Severity",
                table: "AttackPatterns",
                columns: new[] { "AttackType", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_FirstDetectedAt",
                table: "AttackPatterns",
                column: "FirstDetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_IsActive",
                table: "AttackPatterns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_IsActive_LastDetectedAt",
                table: "AttackPatterns",
                columns: new[] { "IsActive", "LastDetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_IsBlocked",
                table: "AttackPatterns",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_LastDetectedAt",
                table: "AttackPatterns",
                column: "LastDetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_Severity",
                table: "AttackPatterns",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_SourceIdentifier",
                table: "AttackPatterns",
                column: "SourceIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_AttackPatterns_SourceIdentifier_AttackType",
                table: "AttackPatterns",
                columns: new[] { "SourceIdentifier", "AttackType" });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_BlacklistedAt",
                table: "BlacklistedTokens",
                column: "BlacklistedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_CreatedByUserId",
                table: "BlacklistedTokens",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_ExpiresAt",
                table: "BlacklistedTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_IsActive",
                table: "BlacklistedTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_TokenId_IsActive",
                table: "BlacklistedTokens",
                columns: new[] { "TokenId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_TokenId_Unique",
                table: "BlacklistedTokens",
                column: "TokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_UserId",
                table: "BlacklistedTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CreatedAt",
                table: "SecurityEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType",
                table: "SecurityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType_CreatedAt",
                table: "SecurityEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IPAddress",
                table: "SecurityEvents",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Success",
                table: "SecurityEvents",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivityAt",
                table: "UserSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken_Unique",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsActive",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttackPatterns");

            migrationBuilder.DropTable(
                name: "BlacklistedTokens");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "UserSessions");
        }
    }
}
