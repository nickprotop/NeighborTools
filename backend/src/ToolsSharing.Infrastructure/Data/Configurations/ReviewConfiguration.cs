using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReviewerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(r => r.RevieweeId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Comment)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Type)
            .IsRequired();

        // Configure relationships with explicit foreign keys and navigation properties
        builder.HasOne(r => r.Reviewer)
            .WithMany(u => u.ReviewsGiven)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reviewee)
            .WithMany(u => u.ReviewsReceived)
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Tool)
            .WithMany(t => t.Reviews)
            .HasForeignKey(r => r.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Rental)
            .WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.ReviewerId);
        builder.HasIndex(r => r.RevieweeId);
        builder.HasIndex(r => r.ToolId);
        builder.HasIndex(r => r.RentalId);
        builder.HasIndex(r => r.Type);
    }
}