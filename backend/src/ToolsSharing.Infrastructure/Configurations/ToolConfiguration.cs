using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Brand)
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .HasMaxLength(50);

        builder.Property(x => x.DailyRate)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.WeeklyRate)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.MonthlyRate)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.DepositRequired)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Condition)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Location)
            .HasMaxLength(200);

        // Relationships
        builder.HasOne(x => x.Owner)
            .WithMany(x => x.OwnedTools)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Images)
            .WithOne(x => x.Tool)
            .HasForeignKey(x => x.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Rentals)
            .WithOne(x => x.Tool)
            .HasForeignKey(x => x.ToolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Location);
        builder.HasIndex(x => x.IsAvailable);
        builder.HasIndex(x => x.OwnerId);
    }
}