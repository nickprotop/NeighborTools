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
                
            // Enhanced location fields (Phase 1 - Comprehensive Location System)
            builder.Property(b => b.LocationDisplay)
                .HasMaxLength(255);
                
            builder.Property(b => b.LocationArea)
                .HasMaxLength(100);
                
            builder.Property(b => b.LocationCity)
                .HasMaxLength(100);
                
            builder.Property(b => b.LocationState)
                .HasMaxLength(100);
                
            builder.Property(b => b.LocationCountry)
                .HasMaxLength(100);
                
            builder.Property(b => b.LocationLat)
                .HasColumnType("decimal(10,8)");
                
            builder.Property(b => b.LocationLng)
                .HasColumnType("decimal(11,8)");
                
            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasIndex(b => b.UserId);
            builder.HasIndex(b => b.Category);
            builder.HasIndex(b => b.IsPublished);
            builder.HasIndex(b => b.IsFeatured);
            
            // Enhanced location indexes (Phase 1 - Comprehensive Location System)
            builder.HasIndex(b => new { b.LocationLat, b.LocationLng })
                .HasDatabaseName("IX_Bundles_LocationCoordinates");
                
            builder.HasIndex(b => b.LocationArea)
                .HasDatabaseName("IX_Bundles_LocationArea");
                
            builder.HasIndex(b => b.LocationCity)
                .HasDatabaseName("IX_Bundles_LocationCity");
                
            builder.HasIndex(b => new { b.LocationCity, b.LocationState })
                .HasDatabaseName("IX_Bundles_LocationCityState");
                
            // Composite index for proximity searches with availability filtering
            builder.HasIndex(b => new { b.LocationLat, b.LocationLng, b.IsPublished, b.IsApproved })
                .HasDatabaseName("IX_Bundles_LocationAvailability");
            
            // Soft delete filter
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}