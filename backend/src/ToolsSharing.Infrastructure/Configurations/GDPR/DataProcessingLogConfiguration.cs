using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(dpl => dpl.DataSources)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(dpl => dpl.DataRecipients)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>?>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? null : c.ToList()));

        // Relationships - Allow null UserId for anonymous users
        builder.HasOne(dpl => dpl.User)
            .WithMany(u => u.ProcessingLogs)
            .HasForeignKey(dpl => dpl.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(dpl => new { dpl.UserId, dpl.ProcessingDate })
            .HasDatabaseName("IX_DataProcessingLogs_User_Date");

        builder.HasIndex(dpl => dpl.ActivityType)
            .HasDatabaseName("IX_DataProcessingLogs_ActivityType");
    }
}