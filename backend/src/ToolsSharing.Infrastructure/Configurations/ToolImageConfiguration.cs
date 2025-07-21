using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class ToolImageConfiguration : IEntityTypeConfiguration<ToolImage>
{
    public void Configure(EntityTypeBuilder<ToolImage> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(2000);
            
        builder.Property(x => x.AltText)
            .HasMaxLength(500);
            
        builder.HasOne(x => x.Tool)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ToolId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(x => x.ToolId);
        builder.HasIndex(x => x.IsPrimary);
    }
}