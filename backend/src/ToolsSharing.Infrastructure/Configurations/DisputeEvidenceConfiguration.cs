using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class DisputeEvidenceConfiguration : IEntityTypeConfiguration<DisputeEvidence>
{
    public void Configure(EntityTypeBuilder<DisputeEvidence> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.StoragePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.UploadedBy)
            .IsRequired()
            .HasMaxLength(450); // Same as User.Id

        builder.Property(e => e.Tags)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(e => e.Dispute)
            .WithMany()
            .HasForeignKey(e => e.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.DisputeId);
        builder.HasIndex(e => e.UploadedBy);
        builder.HasIndex(e => e.UploadedAt);

        builder.ToTable("DisputeEvidence");
    }
}