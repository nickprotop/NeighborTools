using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Infrastructure.Configurations.GDPR;

public class PrivacyPolicyVersionConfiguration : IEntityTypeConfiguration<PrivacyPolicyVersion>
{
    public void Configure(EntityTypeBuilder<PrivacyPolicyVersion> builder)
    {
        builder.HasKey(ppv => ppv.Id);

        builder.Property(ppv => ppv.Version)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ppv => ppv.Content)
            .IsRequired();

        builder.Property(ppv => ppv.EffectiveDate)
            .IsRequired();

        builder.Property(ppv => ppv.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(ppv => ppv.CreatedByUser)
            .WithMany()
            .HasForeignKey(ppv => ppv.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ppv => ppv.Version)
            .IsUnique()
            .HasDatabaseName("IX_PrivacyPolicyVersions_Version");

        builder.HasIndex(ppv => ppv.IsActive)
            .HasDatabaseName("IX_PrivacyPolicyVersions_IsActive");

        builder.HasIndex(ppv => ppv.EffectiveDate)
            .HasDatabaseName("IX_PrivacyPolicyVersions_EffectiveDate");
    }
}