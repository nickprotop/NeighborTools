using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Infrastructure.Configurations.GDPR;

public class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
    public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
    {
        builder.HasKey(dsr => dsr.Id);

        builder.Property(dsr => dsr.RequestType)
            .IsRequired();

        builder.Property(dsr => dsr.RequestDate)
            .IsRequired();

        builder.Property(dsr => dsr.Status)
            .IsRequired();

        builder.Property(dsr => dsr.RequestDetails)
            .HasMaxLength(1000);

        builder.Property(dsr => dsr.ResponseDetails)
            .HasMaxLength(2000);

        builder.Property(dsr => dsr.DataExportPath)
            .HasMaxLength(500);

        builder.Property(dsr => dsr.VerificationMethod)
            .HasMaxLength(100);

        builder.Property(dsr => dsr.RejectionReason)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(dsr => dsr.User)
            .WithMany(u => u.DataRequests)
            .HasForeignKey(dsr => dsr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dsr => dsr.ProcessedByUser)
            .WithMany()
            .HasForeignKey(dsr => dsr.ProcessedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(dsr => new { dsr.UserId, dsr.RequestDate })
            .HasDatabaseName("IX_DataSubjectRequests_User_Date");

        builder.HasIndex(dsr => dsr.Status)
            .HasDatabaseName("IX_DataSubjectRequests_Status");
    }
}