using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities.GDPR;
using System.Text.Json;

namespace ToolsSharing.Infrastructure.Configurations.GDPR;

public class DataProcessingLogConfiguration : IEntityTypeConfiguration<DataProcessingLog>
{
    public void Configure(EntityTypeBuilder<DataProcessingLog> builder)
    {
        builder.HasKey(dpl => dpl.Id);

        builder.Property(dpl => dpl.ActivityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(dpl => dpl.ProcessingPurpose)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(dpl => dpl.LegalBasis)
            .IsRequired();

        builder.Property(dpl => dpl.RetentionPeriod)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(dpl => dpl.ProcessingDate)
            .IsRequired();

        builder.Property(dpl => dpl.IPAddress)
            .HasMaxLength(45); // Support IPv6

        builder.Property(dpl => dpl.UserAgent)
            .HasMaxLength(500);

        // Configure List<string> properties as JSON
        builder.Property(dpl => dpl.DataCategories)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(dpl => dpl.DataSources)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(dpl => dpl.DataRecipients)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
            );

        // Relationships
        builder.HasOne(dpl => dpl.User)
            .WithMany(u => u.ProcessingLogs)
            .HasForeignKey(dpl => dpl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(dpl => new { dpl.UserId, dpl.ProcessingDate })
            .HasDatabaseName("IX_DataProcessingLogs_User_Date");

        builder.HasIndex(dpl => dpl.ActivityType)
            .HasDatabaseName("IX_DataProcessingLogs_ActivityType");
    }
}