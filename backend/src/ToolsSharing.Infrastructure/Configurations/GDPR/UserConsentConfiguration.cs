using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Infrastructure.Configurations.GDPR;

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.ConsentType)
            .IsRequired();

        builder.Property(uc => uc.ConsentGiven)
            .IsRequired();

        builder.Property(uc => uc.ConsentDate)
            .IsRequired();

        builder.Property(uc => uc.ConsentSource)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(uc => uc.ConsentVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(uc => uc.IPAddress)
            .HasMaxLength(45); // Support IPv6

        builder.Property(uc => uc.UserAgent)
            .HasMaxLength(500);

        builder.Property(uc => uc.WithdrawalReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(uc => uc.User)
            .WithMany(u => u.Consents)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(uc => new { uc.UserId, uc.ConsentType, uc.ConsentDate })
            .HasDatabaseName("IX_UserConsents_User_Type_Date");
    }
}