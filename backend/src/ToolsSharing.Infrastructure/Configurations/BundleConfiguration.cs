using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations
{
    public class BundleConfiguration : IEntityTypeConfiguration<Bundle>
    {
        public void Configure(EntityTypeBuilder<Bundle> builder)
        {
            builder.HasKey(b => b.Id);
            
            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(b => b.Description)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(b => b.Guidelines)
                .HasMaxLength(5000);
                
            builder.Property(b => b.RequiredSkillLevel)
                .HasMaxLength(50)
                .HasDefaultValue("Beginner");
                
            builder.Property(b => b.Category)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(b => b.Tags)
                .HasMaxLength(500);
                
            builder.Property(b => b.ImageUrl)
                .HasMaxLength(2000);
                
            builder.Property(b => b.BundleDiscount)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);
                
            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasIndex(b => b.UserId);
            builder.HasIndex(b => b.Category);
            builder.HasIndex(b => b.IsPublished);
            builder.HasIndex(b => b.IsFeatured);
            
            // Soft delete filter
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}