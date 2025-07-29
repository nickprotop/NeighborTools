using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class LocationSearchLogConfiguration : IEntityTypeConfiguration<LocationSearchLog>
{
    public void Configure(EntityTypeBuilder<LocationSearchLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TargetId)
            .HasMaxLength(450); // Same as IdentityUser.Id max length

        builder.Property(x => x.SearchQuery)
            .HasMaxLength(500);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(2000);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.SessionId)
            .HasMaxLength(100);

        builder.Property(x => x.SuspiciousReason)
            .HasMaxLength(1000);

        builder.Property(x => x.SearchLat)
            .HasColumnType("decimal(10,8)");

        builder.Property(x => x.SearchLng)
            .HasColumnType("decimal(11,8)");

        builder.Property(x => x.SearchRadiusKm)
            .HasColumnType("decimal(6,2)");

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for triangulation detection and analysis
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_LocationSearchLog_UserId");

        builder.HasIndex(x => x.IpAddress)
            .HasDatabaseName("IX_LocationSearchLog_IpAddress");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_LocationSearchLog_SessionId");

        builder.HasIndex(x => x.TargetId)
            .HasDatabaseName("IX_LocationSearchLog_TargetId");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_LocationSearchLog_CreatedAt");

        builder.HasIndex(x => x.IsSuspicious)
            .HasDatabaseName("IX_LocationSearchLog_IsSuspicious");

        // Composite indexes for triangulation pattern detection
        builder.HasIndex(x => new { x.UserId, x.TargetId, x.CreatedAt })
            .HasDatabaseName("IX_LocationSearchLog_UserTargetTime");

        builder.HasIndex(x => new { x.IpAddress, x.TargetId, x.CreatedAt })
            .HasDatabaseName("IX_LocationSearchLog_IpTargetTime");

        builder.HasIndex(x => new { x.SessionId, x.CreatedAt })
            .HasDatabaseName("IX_LocationSearchLog_SessionTime");

        // Geographic search pattern analysis
        builder.HasIndex(x => new { x.SearchLat, x.SearchLng, x.CreatedAt })
            .HasDatabaseName("IX_LocationSearchLog_LocationTime");
    }
}