using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Infrastructure.Configurations.GDPR;

public class CookieConsentConfiguration : IEntityTypeConfiguration<CookieConsent>
{
    public void Configure(EntityTypeBuilder<CookieConsent> builder)
    {
        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.SessionId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(cc => cc.CookieCategory)
            .IsRequired();

        builder.Property(cc => cc.ConsentGiven)
            .IsRequired();

        builder.Property(cc => cc.ConsentDate)
            .IsRequired();

        builder.Property(cc => cc.ExpiryDate)
            .IsRequired();

        builder.Property(cc => cc.IPAddress)
            .HasMaxLength(45); // Support IPv6

        builder.Property(cc => cc.UserAgent)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(cc => cc.User)
            .WithMany(u => u.CookieConsents)
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(cc => new { cc.SessionId, cc.CookieCategory })
            .HasDatabaseName("IX_CookieConsents_Session_Category");

        builder.HasIndex(cc => new { cc.UserId, cc.CookieCategory })
            .HasDatabaseName("IX_CookieConsents_User_Category");

        builder.HasIndex(cc => cc.ExpiryDate)
            .HasDatabaseName("IX_CookieConsents_ExpiryDate");
    }
}