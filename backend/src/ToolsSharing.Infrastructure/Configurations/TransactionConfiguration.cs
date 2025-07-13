using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id);
            
        builder.Property(t => t.RentalId)
            .IsRequired();
            
        builder.Property(t => t.RentalAmount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(t => t.SecurityDeposit)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(t => t.CommissionRate)
            .IsRequired()
            .HasPrecision(5, 4); // e.g., 0.1000 for 10%
            
        builder.Property(t => t.CommissionAmount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(t => t.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(t => t.OwnerPayoutAmount)
            .IsRequired()
            .HasPrecision(18, 2);
            
        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");
            
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(t => t.DisputeReason)
            .HasMaxLength(1000);
            
        // Relationships
        builder.HasOne(t => t.Rental)
            .WithOne(r => r.Transaction)
            .HasForeignKey<Transaction>(t => t.RentalId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(t => t.Payments)
            .WithOne()
            .HasForeignKey(p => p.RentalId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Indexes
        builder.HasIndex(t => t.RentalId)
            .IsUnique();
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.PayoutScheduledAt);
        builder.HasIndex(t => t.HasDispute);
    }
}