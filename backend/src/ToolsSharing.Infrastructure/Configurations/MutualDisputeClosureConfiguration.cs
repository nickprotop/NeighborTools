using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class MutualDisputeClosureConfiguration : IEntityTypeConfiguration<MutualDisputeClosure>
{
    public void Configure(EntityTypeBuilder<MutualDisputeClosure> builder)
    {
        builder.ToTable("MutualDisputeClosures");

        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Id)
            .ValueGeneratedOnAdd();

        builder.Property(mc => mc.DisputeId)
            .IsRequired();

        builder.Property(mc => mc.InitiatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(mc => mc.ResponseRequiredFromUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(mc => mc.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(mc => mc.ProposedResolution)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(mc => mc.ResolutionDetails)
            .HasMaxLength(2000);

        builder.Property(mc => mc.AgreedRefundAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(mc => mc.RefundRecipient)
            .HasConversion<int>();

        builder.Property(mc => mc.CreatedAt)
            .IsRequired();

        builder.Property(mc => mc.ExpiresAt)
            .IsRequired();

        builder.Property(mc => mc.ResponseMessage)
            .HasMaxLength(1000);

        builder.Property(mc => mc.RejectionReason)
            .HasMaxLength(500);

        builder.Property(mc => mc.ReviewedByAdminId)
            .HasMaxLength(450);

        builder.Property(mc => mc.AdminNotes)
            .HasMaxLength(1000);

        builder.Property(mc => mc.RefundTransactionId)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(mc => mc.Dispute)
            .WithMany()
            .HasForeignKey(mc => mc.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.InitiatedByUser)
            .WithMany()
            .HasForeignKey(mc => mc.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mc => mc.ResponseRequiredFromUser)
            .WithMany()
            .HasForeignKey(mc => mc.ResponseRequiredFromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mc => mc.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(mc => mc.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(mc => mc.AuditLogs)
            .WithOne(al => al.MutualClosure)
            .HasForeignKey(al => al.MutualClosureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(mc => mc.DisputeId)
            .HasDatabaseName("IX_MutualDisputeClosures_DisputeId");

        builder.HasIndex(mc => mc.InitiatedByUserId)
            .HasDatabaseName("IX_MutualDisputeClosures_InitiatedByUserId");

        builder.HasIndex(mc => mc.ResponseRequiredFromUserId)
            .HasDatabaseName("IX_MutualDisputeClosures_ResponseRequiredFromUserId");

        builder.HasIndex(mc => mc.Status)
            .HasDatabaseName("IX_MutualDisputeClosures_Status");

        builder.HasIndex(mc => mc.CreatedAt)
            .HasDatabaseName("IX_MutualDisputeClosures_CreatedAt");

        builder.HasIndex(mc => mc.ExpiresAt)
            .HasDatabaseName("IX_MutualDisputeClosures_ExpiresAt");

        // Composite indexes for common queries
        builder.HasIndex(mc => new { mc.Status, mc.ExpiresAt })
            .HasDatabaseName("IX_MutualDisputeClosures_Status_ExpiresAt");

        builder.HasIndex(mc => new { mc.InitiatedByUserId, mc.Status })
            .HasDatabaseName("IX_MutualDisputeClosures_InitiatedByUserId_Status");

        builder.HasIndex(mc => new { mc.ResponseRequiredFromUserId, mc.Status })
            .HasDatabaseName("IX_MutualDisputeClosures_ResponseRequiredFromUserId_Status");

        // Unique constraint to prevent multiple active mutual closures per dispute
        builder.HasIndex(mc => new { mc.DisputeId, mc.Status })
            .HasDatabaseName("IX_MutualDisputeClosures_DisputeId_Status_Unique")
            .HasFilter("[Status] = 0"); // Only for Pending status
    }
}

public class MutualClosureAuditLogConfiguration : IEntityTypeConfiguration<MutualClosureAuditLog>
{
    public void Configure(EntityTypeBuilder<MutualClosureAuditLog> builder)
    {
        builder.ToTable("MutualClosureAuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Id)
            .ValueGeneratedOnAdd();

        builder.Property(al => al.MutualClosureId)
            .IsRequired();

        builder.Property(al => al.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(al => al.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(al => al.Metadata)
            .HasMaxLength(1000);

        builder.Property(al => al.CreatedAt)
            .IsRequired();

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(al => al.MutualClosure)
            .WithMany(mc => mc.AuditLogs)
            .HasForeignKey(al => al.MutualClosureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(al => al.MutualClosureId)
            .HasDatabaseName("IX_MutualClosureAuditLogs_MutualClosureId");

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_MutualClosureAuditLogs_UserId");

        builder.HasIndex(al => al.ActionType)
            .HasDatabaseName("IX_MutualClosureAuditLogs_ActionType");

        builder.HasIndex(al => al.CreatedAt)
            .HasDatabaseName("IX_MutualClosureAuditLogs_CreatedAt");

        // Composite index for audit trail queries
        builder.HasIndex(al => new { al.MutualClosureId, al.CreatedAt })
            .HasDatabaseName("IX_MutualClosureAuditLogs_MutualClosureId_CreatedAt");
    }
}