using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations
{
    public class BundleRentalConfiguration : IEntityTypeConfiguration<BundleRental>
    {
        public void Configure(EntityTypeBuilder<BundleRental> builder)
        {
            builder.HasKey(br => br.Id);
            
            builder.Property(br => br.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
                
            builder.Property(br => br.TotalCost)
                .HasPrecision(10, 2);
                
            builder.Property(br => br.BundleDiscountAmount)
                .HasPrecision(10, 2);
                
            builder.Property(br => br.FinalCost)
                .HasPrecision(10, 2);
                
            builder.Property(br => br.RenterNotes)
                .HasMaxLength(1000);
                
            builder.Property(br => br.OwnerNotes)
                .HasMaxLength(1000);
                
            builder.HasOne(br => br.Bundle)
                .WithMany(b => b.BundleRentals)
                .HasForeignKey(br => br.BundleId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(br => br.RenterUser)
                .WithMany()
                .HasForeignKey(br => br.RenterUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(br => br.ToolRentals)
                .WithOne(r => r.BundleRental)
                .HasForeignKey(r => r.BundleRentalId)
                .OnDelete(DeleteBehavior.SetNull);
                
            builder.HasIndex(br => br.BundleId);
            builder.HasIndex(br => br.RenterUserId);
            builder.HasIndex(br => br.Status);
            builder.HasIndex(br => br.RentalDate);
            
            // Soft delete filter
            builder.HasQueryFilter(br => !br.IsDeleted);
        }
    }
}