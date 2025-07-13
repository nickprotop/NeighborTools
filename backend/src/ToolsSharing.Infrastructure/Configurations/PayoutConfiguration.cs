using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id);
            
        builder.Property(p => p.RecipientId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(p => p.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");
            
        builder.Property(p => p.PlatformFee)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.NetAmount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(p => p.PayoutMethod)
            .HasMaxLength(50);
            
        builder.Property(p => p.PayoutDestination)
            .HasMaxLength(500); // Will be encrypted
            
        builder.Property(p => p.ExternalPayoutId)
            .HasMaxLength(255);
            
        builder.Property(p => p.ExternalBatchId)
            .HasMaxLength(255);
            
        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);
            
        builder.Property(p => p.Metadata)
            .HasColumnType("JSON");
            
        // Relationships
        builder.HasOne(p => p.Recipient)
            .WithMany(u => u.Payouts)
            .HasForeignKey(p => p.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Many-to-many relationship with transactions (to be configured)
        builder.HasMany(p => p.Transactions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PayoutTransactions",
                j => j.HasOne<Transaction>().WithMany().HasForeignKey("TransactionId"),
                j => j.HasOne<Payout>().WithMany().HasForeignKey("PayoutId"));
            
        // Indexes
        builder.HasIndex(p => p.RecipientId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ScheduledAt);
        builder.HasIndex(p => p.ExternalPayoutId);
        builder.HasIndex(p => new { p.Provider, p.ExternalPayoutId })
            .IsUnique()
            .HasFilter("ExternalPayoutId IS NOT NULL");
    }
}