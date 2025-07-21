using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations
{
    public class BundleToolConfiguration : IEntityTypeConfiguration<BundleTool>
    {
        public void Configure(EntityTypeBuilder<BundleTool> builder)
        {
            builder.HasKey(bt => bt.Id);
            
            builder.Property(bt => bt.UsageNotes)
                .HasMaxLength(1000);
                
            builder.Property(bt => bt.QuantityNeeded)
                .HasDefaultValue(1);
                
            builder.HasOne(bt => bt.Bundle)
                .WithMany(b => b.BundleTools)
                .HasForeignKey(bt => bt.BundleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(bt => bt.Tool)
                .WithMany()
                .HasForeignKey(bt => bt.ToolId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Ensure a tool can only be added once to a bundle
            builder.HasIndex(bt => new { bt.BundleId, bt.ToolId })
                .IsUnique();
                
            builder.HasIndex(bt => bt.ToolId);
            
            // Soft delete filter
            builder.HasQueryFilter(bt => !bt.IsDeleted);
        }
    }
}