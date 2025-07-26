using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(s => s.SessionToken)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(s => s.DeviceFingerprint)
            .HasMaxLength(500);
        
        builder.Property(s => s.IPAddress)
            .IsRequired()
            .HasMaxLength(45);
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.LastActivityAt)
            .IsRequired();
        
        builder.Property(s => s.ExpiresAt)
            .IsRequired();
        
        builder.Property(s => s.TerminationReason)
            .HasMaxLength(100);
        
        builder.Property(s => s.DeviceName)
            .HasMaxLength(200);
        
        builder.Property(s => s.Platform)
            .HasMaxLength(50);
        
        builder.Property(s => s.Browser)
            .HasMaxLength(100);
        
        builder.Property(s => s.RiskScore)
            .HasColumnType("decimal(5,2)");
        
        // JSON columns
        builder.Property(s => s.GeographicLocation)
            .HasColumnType("json");
        
        // Unique constraint on session token
        builder.HasIndex(s => s.SessionToken)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_SessionToken_Unique");
        
        // Indexes for performance
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");
        
        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_UserSessions_IsActive");
        
        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_UserSessions_ExpiresAt");
        
        builder.HasIndex(s => s.LastActivityAt)
            .HasDatabaseName("IX_UserSessions_LastActivityAt");
        
        builder.HasIndex(s => new { s.UserId, s.IsActive })
            .HasDatabaseName("IX_UserSessions_UserId_IsActive");
        
        // Foreign key relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}