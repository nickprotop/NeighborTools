using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.HasKey(ma => ma.Id);
        
        builder.Property(ma => ma.FileName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(ma => ma.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(ma => ma.ContentType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(ma => ma.StoragePath)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(ma => ma.ScanResult)
            .HasMaxLength(1000);
        
        // Indexes for performance
        builder.HasIndex(ma => ma.MessageId);
        builder.HasIndex(ma => ma.CreatedAt);
        
        // Foreign key relationships
        builder.HasOne(ma => ma.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ma => ma.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}