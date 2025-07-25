using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        // Primary key
        builder.HasKey(f => f.Id);

        // Properties
        builder.Property(f => f.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard ASP.NET Core Identity user ID length

        builder.Property(f => f.ToolId)
            .IsRequired(false); // Now optional

        builder.Property(f => f.BundleId)
            .IsRequired(false); // Optional

        builder.Property(f => f.FavoriteType)
            .IsRequired()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Tool)
            .WithMany(t => t.FavoritedBy)
            .HasForeignKey(f => f.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Bundle)
            .WithMany() // No navigation property in Bundle yet
            .HasForeignKey(f => f.BundleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraints - user can only favorite a tool or bundle once
        builder.HasIndex(f => new { f.UserId, f.ToolId })
            .IsUnique()
            .HasDatabaseName("IX_Favorites_UserId_ToolId")
            .HasFilter("[ToolId] IS NOT NULL");

        builder.HasIndex(f => new { f.UserId, f.BundleId })
            .IsUnique()
            .HasDatabaseName("IX_Favorites_UserId_BundleId")
            .HasFilter("[BundleId] IS NOT NULL");

        // Additional indexes for performance
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("IX_Favorites_UserId");

        builder.HasIndex(f => f.ToolId)
            .HasDatabaseName("IX_Favorites_ToolId");

        builder.HasIndex(f => f.BundleId)
            .HasDatabaseName("IX_Favorites_BundleId");

        builder.HasIndex(f => f.FavoriteType)
            .HasDatabaseName("IX_Favorites_FavoriteType");
    }
}