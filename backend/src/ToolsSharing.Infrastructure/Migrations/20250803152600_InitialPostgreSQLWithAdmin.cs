using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQLWithAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "text", nullable: true),
                    LocationDisplay = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LocationArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationLat = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    LocationLng = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    LocationPrecisionRadius = table.Column<int>(type: "integer", nullable: true),
                    LocationSource = table.Column<int>(type: "integer", nullable: true),
                    LocationPrivacyLevel = table.Column<int>(type: "integer", nullable: false),
                    LocationUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataProcessingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    DataRetentionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastConsentUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GDPROptOut = table.Column<bool>(type: "boolean", nullable: false),
                    DataPortabilityRequested = table.Column<bool>(type: "boolean", nullable: false),
                    AnonymizationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsOfServiceAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    TermsAcceptedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsVersion = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttackPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttackData = table.Column<string>(type: "json", nullable: true),
                    FirstDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    GeographicData = table.Column<string>(type: "json", nullable: true),
                    UserAgentPatterns = table.Column<string>(type: "json", nullable: true),
                    SuccessfulAttempts = table.Column<int>(type: "integer", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlacklistedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    BlacklistedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalData = table.Column<string>(type: "json", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Guidelines = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    RequiredSkillLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Beginner"),
                    EstimatedProjectDuration = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LocationDisplay = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LocationArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationLat = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    LocationLng = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    LocationPrecisionRadius = table.Column<int>(type: "integer", nullable: true),
                    LocationSource = table.Column<int>(type: "integer", nullable: true),
                    LocationPrivacyLevel = table.Column<int>(type: "integer", nullable: false),
                    LocationUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocationInheritanceOption = table.Column<int>(type: "integer", nullable: false),
                    BundleDiscount = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    PendingApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bundles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CookieConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CookieCategory = table.Column<int>(type: "integer", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "DataProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    ActivityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataCategories = table.Column<string>(type: "text", nullable: false),
                    ProcessingPurpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LegalBasis = table.Column<int>(type: "integer", nullable: false),
                    DataSources = table.Column<string>(type: "text", nullable: false),
                    DataRecipients = table.Column<string>(type: "text", nullable: true),
                    RetentionPeriod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RequestType = table.Column<int>(type: "integer", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "text", nullable: true),
                    DataExportPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificationMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "LocationSearchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    TargetId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SearchType = table.Column<int>(type: "integer", nullable: false),
                    SearchLat = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    SearchLng = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    SearchRadiusKm = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    SearchQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    SuspiciousReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "PaymentSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PreferredPayoutMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayPalEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CustomCommissionRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    IsCommissionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutSchedule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayoutDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    PayoutDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    MinimumPayoutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 10.00m),
                    TaxInfoProvided = table.Column<bool>(type: "boolean", nullable: false),
                    TaxIdType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxIdLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusinessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPayoutVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotifyOnPaymentReceived = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnPayoutSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnPayoutFailed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyPolicyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GeographicLocation = table.Column<string>(type: "json", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ResponseAction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdditionalData = table.Column<string>(type: "json", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PatternData = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    FirstDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelatedPaymentIds = table.Column<string>(type: "text", nullable: true),
                    RelatedUserIds = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvestigationNotes = table.Column<string>(type: "text", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserSuspended = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WeeklyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MonthlyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DepositRequired = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    LeadTimeHours = table.Column<int>(type: "integer", nullable: true),
                    LocationDisplay = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LocationArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationLat = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    LocationLng = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    LocationPrecisionRadius = table.Column<int>(type: "integer", nullable: true),
                    LocationSource = table.Column<int>(type: "integer", nullable: true),
                    LocationPrivacyLevel = table.Column<int>(type: "integer", nullable: false),
                    LocationUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocationInheritanceOption = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    PendingApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tools_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConsentType = table.Column<int>(type: "integer", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WithdrawnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    GeographicLocation = table.Column<string>(type: "json", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TerminationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ActivityCount = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ShowProfilePicture = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowRealName = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowLocation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowPhoneNumber = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShowEmail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShowStatistics = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailRentalRequests = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailRentalUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailMessages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailMarketing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailSecurityAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PushMessages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PushReminders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PushRentalRequests = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PushRentalUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "system"),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    AutoApproveRentals = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RentalLeadTime = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    RequireDeposit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultDepositPercentage = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0.20m),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LoginAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowDirectMessages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowRentalInquiries = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowOnlineStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "VelocityLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LimitType = table.Column<int>(type: "integer", nullable: false),
                    TimeWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AmountLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionLimit = table.Column<int>(type: "integer", nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentTransactions = table.Column<int>(type: "integer", nullable: false),
                    WindowStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CustomReason = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VelocityLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VelocityLimits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BundleRentals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterUserId = table.Column<string>(type: "text", nullable: false),
                    RentalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BundleDiscountAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FinalCost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    RenterNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OwnerNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleRentals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleRentals_AspNetUsers_RenterUserId",
                        column: x => x.RenterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleRentals_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BundleTools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OrderInBundle = table.Column<int>(type: "integer", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    QuantityNeeded = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleTools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleTools_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BundleTools_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: true),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: true),
                    FavoriteType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AltText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolImages_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rentals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterId = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PickupDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnConditionNotes = table.Column<string>(type: "text", nullable: true),
                    ReturnedByUserId = table.Column<string>(type: "text", nullable: true),
                    DisputeDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    BundleRentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rentals_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rentals_AspNetUsers_RenterId",
                        column: x => x.RenterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rentals_BundleRentals_BundleRentalId",
                        column: x => x.BundleRentalId,
                        principalTable: "BundleRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Rentals_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    PlatformFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PayoutDestination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalPayoutId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalBatchId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "text", nullable: true),
                    PayPalEmail = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payouts_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecipientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: true),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: true),
                    BundleRentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RevieweeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_RevieweeId",
                        column: x => x.RevieweeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_BundleRentals_BundleRentalId",
                        column: x => x.BundleRentalId,
                        principalTable: "BundleRentals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleRentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentProviderId = table.Column<string>(type: "text", nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "text", nullable: true),
                    RentalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerPayoutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayoutScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayoutCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepositRefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasDispute = table.Column<bool>(type: "boolean", nullable: false),
                    DisputeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisputeOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_BundleRentals_BundleRentalId",
                        column: x => x.BundleRentalId,
                        principalTable: "BundleRentals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PayeeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    ExternalPaymentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalOrderId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalPayerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRefunded = table.Column<bool>(type: "boolean", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_AspNetUsers_PayeeId",
                        column: x => x.PayeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_AspNetUsers_PayerId",
                        column: x => x.PayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Transactions_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayoutTransactions",
                columns: table => new
                {
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutTransactions", x => new { x.PayoutId, x.TransactionId });
                    table.ForeignKey(
                        name: "FK_PayoutTransactions_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayoutTransactions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Disputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatedBy = table.Column<string>(type: "text", nullable: false),
                    InitiatorId = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DisputeAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DisputedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    InitiatedByName = table.Column<string>(type: "text", nullable: false),
                    ExternalDisputeId = table.Column<string>(type: "text", nullable: true),
                    ExternalCaseId = table.Column<string>(type: "text", nullable: true),
                    PayPalReason = table.Column<int>(type: "integer", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<int>(type: "integer", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disputes_AspNetUsers_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Disputes_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Disputes_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FraudChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckType = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CheckDetails = table.Column<string>(type: "JSON", nullable: false),
                    TriggeredRules = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    UserFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    AdminNotified = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FraudChecks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FraudChecks_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisputeEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_AspNetUsers_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisputeMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<string>(type: "text", nullable: false),
                    ToUserId = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Attachments = table.Column<string>(type: "text", nullable: true),
                    IsFromAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SenderId = table.Column<string>(type: "text", nullable: false),
                    SenderName = table.Column<string>(type: "text", nullable: false),
                    SenderRole = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_AspNetUsers_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_AspNetUsers_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DisputeMessages_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MutualDisputeClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ResponseRequiredFromUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedResolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ResolutionDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AgreedRefundAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RefundRecipient = table.Column<int>(type: "integer", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedByAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AdminReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiresPaymentAction = table.Column<bool>(type: "boolean", nullable: false),
                    RefundTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "MutualClosureAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MutualClosureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Participant1Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Participant2Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_Participant1Id",
                        column: x => x.Participant1Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_Participant2Id",
                        column: x => x.Participant2Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Conversations_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RecipientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalContent = table.Column<string>(type: "TEXT", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    IsModerated = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    ModerationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModeratedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RentalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsScanned = table.Column<bool>(type: "boolean", nullable: false),
                    IsSafe = table.Column<bool>(type: "boolean", nullable: false),
                    ScanResult = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

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
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

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
                name: "IX_BundleRentals_BundleId",
                table: "BundleRentals",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleRentals_RentalDate",
                table: "BundleRentals",
                column: "RentalDate");

            migrationBuilder.CreateIndex(
                name: "IX_BundleRentals_RenterUserId",
                table: "BundleRentals",
                column: "RenterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleRentals_Status",
                table: "BundleRentals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_Category",
                table: "Bundles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_IsFeatured",
                table: "Bundles",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_IsPublished",
                table: "Bundles",
                column: "IsPublished");

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
                name: "IX_Bundles_UserId",
                table: "Bundles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleTools_BundleId_ToolId",
                table: "BundleTools",
                columns: new[] { "BundleId", "ToolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleTools_ToolId",
                table: "BundleTools",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageId",
                table: "Conversations",
                column: "LastMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id",
                table: "Conversations",
                column: "Participant1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations",
                columns: new[] { "Participant1Id", "Participant2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant2Id",
                table: "Conversations",
                column: "Participant2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_RentalId",
                table: "Conversations",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ToolId",
                table: "Conversations",
                column: "ToolId");

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
                name: "IX_DisputeEvidence_DisputeId",
                table: "DisputeEvidence",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_UploadedAt",
                table: "DisputeEvidence",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_UploadedBy",
                table: "DisputeEvidence",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_DisputeId",
                table: "DisputeMessages",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_FromUserId",
                table: "DisputeMessages",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_ToUserId",
                table: "DisputeMessages",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_InitiatorId",
                table: "Disputes",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_PaymentId",
                table: "Disputes",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_RentalId",
                table: "Disputes",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_BundleId",
                table: "Favorites",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_FavoriteType",
                table: "Favorites",
                column: "FavoriteType");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_ToolId",
                table: "Favorites",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_BundleId",
                table: "Favorites",
                columns: new[] { "UserId", "BundleId" },
                unique: true,
                filter: "\"BundleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_ToolId",
                table: "Favorites",
                columns: new[] { "UserId", "ToolId" },
                unique: true,
                filter: "\"ToolId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_PaymentId",
                table: "FraudChecks",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_RiskLevel",
                table: "FraudChecks",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_Status",
                table: "FraudChecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FraudChecks_UserId",
                table: "FraudChecks",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_CreatedAt",
                table: "MessageAttachments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_MessageId",
                table: "MessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedAt",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RecipientId",
                table: "Messages",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RecipientId_IsRead",
                table: "Messages",
                columns: new[] { "RecipientId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RentalId",
                table: "Messages",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId_RecipientId_CreatedAt",
                table: "Messages",
                columns: new[] { "SenderId", "RecipientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ToolId",
                table: "Messages",
                column: "ToolId");

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
                filter: "\"Status\" = 0");

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

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalPaymentId",
                table: "Payments",
                column: "ExternalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayeeId",
                table: "Payments",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayerId",
                table: "Payments",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ExternalPaymentId",
                table: "Payments",
                columns: new[] { "Provider", "ExternalPaymentId" },
                unique: true,
                filter: "\"ExternalPaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RentalId",
                table: "Payments",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSettings_IsPayoutVerified",
                table: "PaymentSettings",
                column: "IsPayoutVerified");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSettings_PayPalEmail",
                table: "PaymentSettings",
                column: "PayPalEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSettings_UserId",
                table: "PaymentSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ExternalPayoutId",
                table: "Payouts",
                column: "ExternalPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_Provider_ExternalPayoutId",
                table: "Payouts",
                columns: new[] { "Provider", "ExternalPayoutId" },
                unique: true,
                filter: "\"ExternalPayoutId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_RecipientId",
                table: "Payouts",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_RentalId",
                table: "Payouts",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ScheduledAt",
                table: "Payouts",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_Status",
                table: "Payouts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutTransactions_TransactionId",
                table: "PayoutTransactions",
                column: "TransactionId");

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
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserActive",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

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
                name: "IX_Rentals_BundleRentalId",
                table: "Rentals",
                column: "BundleRentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_EndDate",
                table: "Rentals",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_OwnerId",
                table: "Rentals",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_RenterId",
                table: "Rentals",
                column: "RenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_StartDate",
                table: "Rentals",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Status",
                table: "Rentals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_ToolId",
                table: "Rentals",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BundleId",
                table: "Reviews",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BundleRentalId",
                table: "Reviews",
                column: "BundleRentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RentalId",
                table: "Reviews",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RevieweeId",
                table: "Reviews",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerId",
                table: "Reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ToolId",
                table: "Reviews",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Type",
                table: "Reviews",
                column: "Type");

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
                name: "IX_SuspiciousActivities_UserId",
                table: "SuspiciousActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolImages_IsPrimary",
                table: "ToolImages",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_ToolImages_ToolId",
                table: "ToolImages",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Category",
                table: "Tools",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_IsAvailable",
                table: "Tools",
                column: "IsAvailable");

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
                name: "IX_Tools_OwnerId",
                table: "Tools",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BundleRentalId",
                table: "Transactions",
                column: "BundleRentalId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_HasDispute",
                table: "Transactions",
                column: "HasDispute");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayoutScheduledAt",
                table: "Transactions",
                column: "PayoutScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RentalId",
                table: "Transactions",
                column: "RentalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_User_Type_Date",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentType", "ConsentDate" });

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

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VelocityLimits_UserId",
                table: "VelocityLimits",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Messages_LastMessageId",
                table: "Conversations",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create initial roles
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { "1", "Admin", "ADMIN", "admin-role-stamp" },
                    { "2", "User", "USER", "user-role-stamp" }
                });

            // Create initial admin user
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { 
                    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed",
                    "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled",
                    "LockoutEnabled", "AccessFailedCount", "FirstName", "LastName", "DateOfBirth", "CreatedAt", "UpdatedAt",
                    "IsDeleted", "DataProcessingConsent", "MarketingConsent", "GDPROptOut", "DataPortabilityRequested",
                    "TermsOfServiceAccepted", "TermsAcceptedDate", "TermsVersion", "LocationPrivacyLevel"
                },
                values: new object[] { 
                    "admin-user-initial", "admin@neighbortools.com", "ADMIN@NEIGHBORTOOLS.COM", "admin@neighbortools.com", "ADMIN@NEIGHBORTOOLS.COM", true,
                    "AQAAAAIAAYagAAAAEBx4XEK9RQVI+WdsLNqTQj4nPvHPObUpKz7Ig1v/qTiw/Cr/Ae/XFh0O2ECN7vVVDw==", "admin-security-stamp", "admin-concurrency-stamp", false, false,
                    true, 0, "System", "Administrator", new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 8, 3, 18, 0, 0, DateTimeKind.Utc), new DateTime(2025, 8, 3, 18, 0, 0, DateTimeKind.Utc),
                    false, true, false, false, false,
                    true, new DateTime(2025, 8, 3, 18, 0, 0, DateTimeKind.Utc), "1.0", 0
                });

            // Assign admin role to admin user
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[,]
                {
                    { "admin-user-initial", "1" }, // Admin role
                    { "admin-user-initial", "2" }  // User role
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BundleRentals_AspNetUsers_RenterUserId",
                table: "BundleRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_Bundles_AspNetUsers_UserId",
                table: "Bundles");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_Participant1Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_Participant2Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_RecipientId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_SenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Rentals_AspNetUsers_OwnerId",
                table: "Rentals");

            migrationBuilder.DropForeignKey(
                name: "FK_Rentals_AspNetUsers_RenterId",
                table: "Rentals");

            migrationBuilder.DropForeignKey(
                name: "FK_Tools_AspNetUsers_OwnerId",
                table: "Tools");

            migrationBuilder.DropForeignKey(
                name: "FK_BundleRentals_Bundles_BundleId",
                table: "BundleRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Tools_ToolId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Tools_ToolId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Rentals_Tools_ToolId",
                table: "Rentals");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Messages_LastMessageId",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AttackPatterns");

            migrationBuilder.DropTable(
                name: "BlacklistedTokens");

            migrationBuilder.DropTable(
                name: "BundleTools");

            migrationBuilder.DropTable(
                name: "CookieConsents");

            migrationBuilder.DropTable(
                name: "DataProcessingLogs");

            migrationBuilder.DropTable(
                name: "DataSubjectRequests");

            migrationBuilder.DropTable(
                name: "DisputeEvidence");

            migrationBuilder.DropTable(
                name: "DisputeMessages");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "FraudChecks");

            migrationBuilder.DropTable(
                name: "LocationSearchLogs");

            migrationBuilder.DropTable(
                name: "MessageAttachments");

            migrationBuilder.DropTable(
                name: "MutualClosureAuditLogs");

            migrationBuilder.DropTable(
                name: "PaymentSettings");

            migrationBuilder.DropTable(
                name: "PayoutTransactions");

            migrationBuilder.DropTable(
                name: "PrivacyPolicyVersions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RentalNotifications");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SuspiciousActivities");

            migrationBuilder.DropTable(
                name: "ToolImages");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "VelocityLimits");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "MutualDisputeClosures");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "Disputes");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropTable(
                name: "Tools");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Rentals");

            migrationBuilder.DropTable(
                name: "BundleRentals");
        }
    }
}
