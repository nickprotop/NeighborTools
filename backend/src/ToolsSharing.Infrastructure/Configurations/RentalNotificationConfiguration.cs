using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class RentalNotificationConfiguration : IEntityTypeConfiguration<RentalNotification>
{
    public void Configure(EntityTypeBuilder<RentalNotification> builder)
    {
        builder.HasKey(rn => rn.Id);

        builder.Property(rn => rn.NotificationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rn => rn.RecipientId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rn => rn.Channel)
            .HasMaxLength(20);

        builder.Property(rn => rn.SentAt)
            .IsRequired();

        builder.Property(rn => rn.IsRead)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(rn => rn.Rental)
            .WithMany()
            .HasForeignKey(rn => rn.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rn => rn.Recipient)
            .WithMany()
            .HasForeignKey(rn => rn.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rn => rn.RentalId);
        builder.HasIndex(rn => rn.RecipientId);
        builder.HasIndex(rn => new { rn.RentalId, rn.NotificationType });
        builder.HasIndex(rn => rn.SentAt);
    }
}