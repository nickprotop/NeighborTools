using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Enhanced location fields (Phase 1 - Comprehensive Location System)
        builder.Property(u => u.LocationDisplay)
            .HasMaxLength(255);
            
        builder.Property(u => u.LocationArea)
            .HasMaxLength(100);
            
        builder.Property(u => u.LocationCity)
            .HasMaxLength(100);
            
        builder.Property(u => u.LocationState)
            .HasMaxLength(100);
            
        builder.Property(u => u.LocationCountry)
            .HasMaxLength(100);
            
        builder.Property(u => u.LocationLat)
            .HasColumnType("decimal(10,8)");
            
        builder.Property(u => u.LocationLng)
            .HasColumnType("decimal(11,8)");

        // Enhanced location indexes (Phase 1 - Comprehensive Location System)
        builder.HasIndex(u => new { u.LocationLat, u.LocationLng })
            .HasDatabaseName("IX_Users_LocationCoordinates");
            
        builder.HasIndex(u => u.LocationArea)
            .HasDatabaseName("IX_Users_LocationArea");
            
        builder.HasIndex(u => u.LocationCity)
            .HasDatabaseName("IX_Users_LocationCity");
            
        builder.HasIndex(u => new { u.LocationCity, u.LocationState })
            .HasDatabaseName("IX_Users_LocationCityState");
    }
}