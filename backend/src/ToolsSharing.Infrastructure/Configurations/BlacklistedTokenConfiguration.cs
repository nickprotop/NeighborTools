using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class BlacklistedTokenConfiguration : IEntityTypeConfiguration<BlacklistedToken>
{
    public void Configure(EntityTypeBuilder<BlacklistedToken> builder)
    {
        builder.ToTable("BlacklistedTokens");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.TokenId)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(t => t.UserId)
            .HasMaxLength(450);
        
        builder.Property(t => t.CreatedByUserId)
            .HasMaxLength(450);
        
        builder.Property(t => t.BlacklistedAt)
            .IsRequired();
        
        builder.Property(t => t.ExpiresAt)
            .IsRequired();
        
        builder.Property(t => t.Reason)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.IPAddress)
            .HasMaxLength(45);
        
        builder.Property(t => t.UserAgent)
            .HasMaxLength(500);
        
        builder.Property(t => t.SessionId)
            .HasMaxLength(100);
        
        // JSON column
        builder.Property(t => t.AdditionalData)
            .HasColumnType("json");
        
        // Unique constraint on token ID
        builder.HasIndex(t => t.TokenId)
            .IsUnique()
            .HasDatabaseName("IX_BlacklistedTokens_TokenId_Unique");
        
        // Indexes for performance
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_BlacklistedTokens_UserId");
        
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("IX_BlacklistedTokens_ExpiresAt");
        
        builder.HasIndex(t => t.BlacklistedAt)
            .HasDatabaseName("IX_BlacklistedTokens_BlacklistedAt");
        
        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_BlacklistedTokens_IsActive");
        
        builder.HasIndex(t => new { t.TokenId, t.IsActive })
            .HasDatabaseName("IX_BlacklistedTokens_TokenId_IsActive");
        
        // Foreign key relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}