using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.UserId)
            .HasMaxLength(450);
        
        builder.Property(e => e.UserEmail)
            .HasMaxLength(256);
        
        builder.Property(e => e.IPAddress)
            .IsRequired()
            .HasMaxLength(45);
        
        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);
        
        builder.Property(e => e.SessionId)
            .HasMaxLength(100);
        
        builder.Property(e => e.DeviceFingerprint)
            .HasMaxLength(500);
        
        builder.Property(e => e.RiskScore)
            .HasColumnType("decimal(5,2)");
        
        builder.Property(e => e.ResponseAction)
            .HasMaxLength(100);
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        // JSON columns for complex data
        builder.Property(e => e.GeographicLocation)
            .HasColumnType("json");
        
        builder.Property(e => e.AdditionalData)
            .HasColumnType("json");
        
        // Indexes for performance
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_SecurityEvents_EventType");
        
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SecurityEvents_UserId");
        
        builder.HasIndex(e => e.IPAddress)
            .HasDatabaseName("IX_SecurityEvents_IPAddress");
        
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_SecurityEvents_CreatedAt");
        
        builder.HasIndex(e => e.Success)
            .HasDatabaseName("IX_SecurityEvents_Success");
        
        builder.HasIndex(e => new { e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_SecurityEvents_EventType_CreatedAt");
        
        // Foreign key relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}