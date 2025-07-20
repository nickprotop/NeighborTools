using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolsSharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEssentialAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Essential Admin User - Required for accessing admin panel
            // Password: Admin123! (hashed)
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
                    'AQAAAAIAAYagAAAAENaSYxhAOH+IVf/oWulNX/O4dydxSfnjfF9ibD7VQFKwEYhpEyKCyHO2GNcon+gUDA==',
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove essential admin user
            migrationBuilder.Sql(@"
                DELETE FROM AspNetUserRoles WHERE UserId = 'admin-user-essential-1234567890';
                DELETE FROM AspNetUsers WHERE Id = 'admin-user-essential-1234567890';
            ");
        }
    }
}
