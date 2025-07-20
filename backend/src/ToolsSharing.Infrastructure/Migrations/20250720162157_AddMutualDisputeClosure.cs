using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMutualDisputeClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MutualDisputeClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DisputeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InitiatedByUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseRequiredFromUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProposedResolution = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolutionDetails = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgreedRefundAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RefundRecipient = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResponseMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByAdminId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdminNotes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresPaymentAction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RefundTransactionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutualDisputeClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutualDisputeClosures_AspNetUsers_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MutualDisputeClosures_AspNetUsers_ResponseRequiredFromUserId",
                        column: x => x.ResponseRequiredFromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MutualDisputeClosures_AspNetUsers_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MutualDisputeClosures_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MutualClosureAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MutualClosureId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutualClosureAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutualClosureAuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MutualClosureAuditLogs_MutualDisputeClosures_MutualClosureId",
                        column: x => x.MutualClosureId,
                        principalTable: "MutualDisputeClosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MutualClosureAuditLogs_ActionType",
                table: "MutualClosureAuditLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_MutualClosureAuditLogs_CreatedAt",
                table: "MutualClosureAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MutualClosureAuditLogs_MutualClosureId",
                table: "MutualClosureAuditLogs",
                column: "MutualClosureId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualClosureAuditLogs_MutualClosureId_CreatedAt",
                table: "MutualClosureAuditLogs",
                columns: new[] { "MutualClosureId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MutualClosureAuditLogs_UserId",
                table: "MutualClosureAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_CreatedAt",
                table: "MutualDisputeClosures",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_DisputeId",
                table: "MutualDisputeClosures",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_DisputeId_Status_Unique",
                table: "MutualDisputeClosures",
                columns: new[] { "DisputeId", "Status" },
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_ExpiresAt",
                table: "MutualDisputeClosures",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_InitiatedByUserId",
                table: "MutualDisputeClosures",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_InitiatedByUserId_Status",
                table: "MutualDisputeClosures",
                columns: new[] { "InitiatedByUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_ResponseRequiredFromUserId",
                table: "MutualDisputeClosures",
                column: "ResponseRequiredFromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_ResponseRequiredFromUserId_Status",
                table: "MutualDisputeClosures",
                columns: new[] { "ResponseRequiredFromUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_ReviewedByAdminId",
                table: "MutualDisputeClosures",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_Status",
                table: "MutualDisputeClosures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MutualDisputeClosures_Status_ExpiresAt",
                table: "MutualDisputeClosures",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MutualClosureAuditLogs");

            migrationBuilder.DropTable(
                name: "MutualDisputeClosures");
        }
    }
}
