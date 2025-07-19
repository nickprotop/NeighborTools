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
            .IsRequired();

        // Relationships
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Tool)
            .WithMany(t => t.FavoritedBy)
            .HasForeignKey(f => f.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - user can only favorite a tool once
        builder.HasIndex(f => new { f.UserId, f.ToolId })
            .IsUnique()
            .HasDatabaseName("IX_Favorites_UserId_ToolId");

        // Additional indexes for performance
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("IX_Favorites_UserId");

        builder.HasIndex(f => f.ToolId)
            .HasDatabaseName("IX_Favorites_ToolId");
    }
}