using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class PaymentSettingsConfiguration : IEntityTypeConfiguration<PaymentSettings>
{
    public void Configure(EntityTypeBuilder<PaymentSettings> builder)
    {
        builder.ToTable("PaymentSettings");
        
        builder.HasKey(ps => ps.Id);
        
        builder.Property(ps => ps.Id);
            
        builder.Property(ps => ps.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(ps => ps.PreferredPayoutMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(ps => ps.PayPalEmail)
            .HasMaxLength(255);
            
        builder.Property(ps => ps.StripeAccountId)
            .HasMaxLength(255);
            
        builder.Property(ps => ps.CustomCommissionRate)
            .HasPrecision(5, 4); // e.g., 0.1500 for 15%
            
        builder.Property(ps => ps.PayoutSchedule)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(ps => ps.MinimumPayoutAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(10.00m);
            
        builder.Property(ps => ps.TaxIdType)
            .HasMaxLength(50);
            
        builder.Property(ps => ps.TaxIdLast4)
            .HasMaxLength(4);
            
        builder.Property(ps => ps.BusinessName)
            .HasMaxLength(255);
            
        builder.Property(ps => ps.BusinessType)
            .HasMaxLength(50);
            
        builder.Property(ps => ps.VerificationNotes)
            .HasMaxLength(500);
            
        // Relationships
        builder.HasOne(ps => ps.User)
            .WithOne(u => u.PaymentSettings)
            .HasForeignKey<PaymentSettings>(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Indexes
        builder.HasIndex(ps => ps.UserId)
            .IsUnique();
        builder.HasIndex(ps => ps.PayPalEmail);
        builder.HasIndex(ps => ps.IsPayoutVerified);
    }
}