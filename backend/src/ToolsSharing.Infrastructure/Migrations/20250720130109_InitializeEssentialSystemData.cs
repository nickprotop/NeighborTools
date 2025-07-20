using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitializeEssentialSystemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Essential Roles - Insert only if they don't exist
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES 
                    ('role-admin-guid-1234-567890123456', 'Admin', 'ADMIN', UUID()),
                    ('role-user-guid-1234-567890123456', 'User', 'USER', UUID());
            ");

            // Essential Admin User - Required for accessing admin panel
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO AspNetUsers (
                    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
                    TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
                    Address, City, PostalCode, Country, PublicLocation, DateOfBirth,
                    DataProcessingConsent, MarketingConsent, TermsOfServiceAccepted,
                    TermsAcceptedDate, TermsVersion, CreatedAt, UpdatedAt
                ) VALUES (
                    'admin-user-essential-1234567890',
                    'admin@neighbortools.com',
                    'ADMIN@NEIGHBORTOOLS.COM',
                    'admin@neighbortools.com',
                    'ADMIN@NEIGHBORTOOLS.COM',
                    1,
                    'AQAAAAIAAYagAAAAEGqkdLyOQ2rP3/GfJ5h7V+Y8m3HpNgXN2RwF8L4kJ1pEf0vKXxDo9mZ3VtY5IwQR0Q==',
                    UUID(),
                    UUID(),
                    '+10000000000',
                    1,
                    0,
                    1,
                    0,
                    'System',
                    'Administrator',
                    'System Address',
                    'System City',
                    '00000',
                    'System',
                    'System Location',
                    '1990-01-01',
                    1,
                    0,
                    1,
                    NOW(),
                    '1.0',
                    NOW(),
                    NOW()
                );
            ");

            // Assign admin role to essential admin user
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO AspNetUserRoles (UserId, RoleId)
                SELECT 
                    'admin-user-essential-1234567890',
                    r.Id
                FROM AspNetRoles r 
                WHERE r.Name = 'Admin'
                LIMIT 1;
            ");

            // Also assign user role to essential admin user
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO AspNetUserRoles (UserId, RoleId)
                SELECT 
                    'admin-user-essential-1234567890',
                    r.Id
                FROM AspNetRoles r 
                WHERE r.Name = 'User'
                LIMIT 1;
            ");

            // Tool Categories - Essential for system functionality
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ToolCategories (
                    Id VARCHAR(255) PRIMARY KEY,
                    Name VARCHAR(255) NOT NULL,
                    Description TEXT,
                    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
                );
            ");

            migrationBuilder.Sql(@"
                INSERT IGNORE INTO ToolCategories (Id, Name, Description)
                VALUES 
                    ('power-tools', 'Power Tools', 'Electric and battery-powered tools'),
                    ('hand-tools', 'Hand Tools', 'Manual tools and equipment'),
                    ('ladders', 'Ladders', 'Step ladders and extension ladders'),
                    ('cleaning', 'Cleaning', 'Pressure washers and cleaning equipment'),
                    ('automotive', 'Automotive', 'Car maintenance and repair tools'),
                    ('garden', 'Garden', 'Lawn mowers and gardening equipment'),
                    ('construction', 'Construction', 'Heavy-duty construction tools'),
                    ('electrical', 'Electrical', 'Electrical testing and installation tools'),
                    ('plumbing', 'Plumbing', 'Pipe work and plumbing tools'),
                    ('safety', 'Safety', 'Safety equipment and protective gear');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove tool categories
            migrationBuilder.Sql("DROP TABLE IF EXISTS ToolCategories;");

            // Remove essential admin user
            migrationBuilder.Sql(@"
                DELETE FROM AspNetUserRoles WHERE UserId = 'admin-user-essential-1234567890';
                DELETE FROM AspNetUsers WHERE Id = 'admin-user-essential-1234567890';
            ");

            // Remove essential roles (only if they have the specific IDs we created)
            migrationBuilder.Sql(@"
                DELETE FROM AspNetRoles 
                WHERE Id IN ('role-admin-guid-1234-567890123456', 'role-user-guid-1234-567890123456');
            ");
        }
    }
}
