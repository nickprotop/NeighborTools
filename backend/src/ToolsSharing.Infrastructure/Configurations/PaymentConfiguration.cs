using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id);
            
        builder.Property(p => p.RentalId)
            .IsRequired();
            
        builder.Property(p => p.PayerId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(p => p.PayeeId)
            .HasMaxLength(450);
            
        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
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
            
        builder.Property(p => p.ExternalPaymentId)
            .HasMaxLength(255);
            
        builder.Property(p => p.ExternalOrderId)
            .HasMaxLength(255);
            
        builder.Property(p => p.ExternalPayerId)
            .HasMaxLength(255);
            
        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);
            
        builder.Property(p => p.RefundedAmount)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.RefundReason)
            .HasMaxLength(500);
            
        builder.Property(p => p.Metadata)
            .HasColumnType("JSON");
            
        // Relationships
        builder.HasOne(p => p.Rental)
            .WithMany()
            .HasForeignKey(p => p.RentalId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(p => p.Payer)
            .WithMany(u => u.PaymentsMade)
            .HasForeignKey(p => p.PayerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(p => p.Payee)
            .WithMany(u => u.PaymentsReceived)
            .HasForeignKey(p => p.PayeeId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Indexes
        builder.HasIndex(p => p.RentalId);
        builder.HasIndex(p => p.PayerId);
        builder.HasIndex(p => p.PayeeId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ExternalPaymentId);
        builder.HasIndex(p => new { p.Provider, p.ExternalPaymentId })
            .IsUnique()
            .HasFilter("ExternalPaymentId IS NOT NULL");
    }
}