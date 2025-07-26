using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class AttackPatternConfiguration : IEntityTypeConfiguration<AttackPattern>
{
    public void Configure(EntityTypeBuilder<AttackPattern> builder)
    {
        builder.ToTable("AttackPatterns");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.AttackType)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.SourceIdentifier)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.TargetIdentifier)
            .HasMaxLength(100);
        
        builder.Property(p => p.FirstDetectedAt)
            .IsRequired();
        
        builder.Property(p => p.LastDetectedAt)
            .IsRequired();
        
        builder.Property(p => p.Severity)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(p => p.ResolvedBy)
            .HasMaxLength(200);
        
        builder.Property(p => p.ResolutionNotes)
            .HasMaxLength(500);
        
        builder.Property(p => p.RiskScore)
            .HasColumnType("decimal(5,2)");
        
        builder.Property(p => p.UpdatedAt)
            .IsRequired();
        
        // JSON columns
        builder.Property(p => p.AttackData)
            .HasColumnType("json");
        
        builder.Property(p => p.GeographicData)
            .HasColumnType("json");
        
        builder.Property(p => p.UserAgentPatterns)
            .HasColumnType("json");
        
        // Indexes for performance and analytics
        builder.HasIndex(p => p.AttackType)
            .HasDatabaseName("IX_AttackPatterns_AttackType");
        
        builder.HasIndex(p => p.SourceIdentifier)
            .HasDatabaseName("IX_AttackPatterns_SourceIdentifier");
        
        builder.HasIndex(p => p.Severity)
            .HasDatabaseName("IX_AttackPatterns_Severity");
        
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_AttackPatterns_IsActive");
        
        builder.HasIndex(p => p.IsBlocked)
            .HasDatabaseName("IX_AttackPatterns_IsBlocked");
        
        builder.HasIndex(p => p.FirstDetectedAt)
            .HasDatabaseName("IX_AttackPatterns_FirstDetectedAt");
        
        builder.HasIndex(p => p.LastDetectedAt)
            .HasDatabaseName("IX_AttackPatterns_LastDetectedAt");
        
        builder.HasIndex(p => new { p.AttackType, p.Severity })
            .HasDatabaseName("IX_AttackPatterns_AttackType_Severity");
        
        builder.HasIndex(p => new { p.SourceIdentifier, p.AttackType })
            .HasDatabaseName("IX_AttackPatterns_SourceIdentifier_AttackType");
        
        builder.HasIndex(p => new { p.IsActive, p.LastDetectedAt })
            .HasDatabaseName("IX_AttackPatterns_IsActive_LastDetectedAt");
    }
}