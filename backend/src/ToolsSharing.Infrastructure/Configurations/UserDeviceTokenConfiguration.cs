using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.HasKey(udt => udt.Id);

        builder.Property(udt => udt.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(udt => udt.DeviceToken)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(udt => udt.Platform)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(udt => udt.IsActive)
            .HasDefaultValue(true);

        builder.Property(udt => udt.LastUsed)
            .IsRequired();

        builder.Property(udt => udt.DeviceInfo)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(udt => udt.User)
            .WithMany()
            .HasForeignKey(udt => udt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(udt => udt.UserId);
        builder.HasIndex(udt => udt.DeviceToken);
        builder.HasIndex(udt => new { udt.UserId, udt.IsActive });
    }
}