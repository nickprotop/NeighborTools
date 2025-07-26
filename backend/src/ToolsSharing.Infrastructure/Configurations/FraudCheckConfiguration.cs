using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class FraudCheckConfiguration : IEntityTypeConfiguration<FraudCheck>
{
    public void Configure(EntityTypeBuilder<FraudCheck> builder)
    {
        builder.HasKey(f => f.Id);
        
        // Explicit foreign key configuration to eliminate shadow property
        builder.HasOne(f => f.Payment)
            .WithMany() // Payment doesn't have a collection of FraudChecks
            .HasForeignKey(f => f.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(f => f.User)
            .WithMany() // User doesn't have a collection of FraudChecks  
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Other property configurations
        builder.Property(f => f.RiskScore)
            .HasPrecision(5, 2); // 0-100 with 2 decimal places
            
        builder.Property(f => f.CheckDetails)
            .HasColumnType("JSON");
            
        // Indexes for performance
        builder.HasIndex(f => f.PaymentId);
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.RiskLevel);
        builder.HasIndex(f => f.Status);
    }
}