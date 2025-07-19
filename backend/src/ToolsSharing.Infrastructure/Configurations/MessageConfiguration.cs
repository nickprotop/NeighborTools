using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.SenderId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(m => m.RecipientId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(m => m.Subject)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("TEXT");
            
        builder.Property(m => m.OriginalContent)
            .HasColumnType("TEXT");
            
        builder.Property(m => m.ModerationReason)
            .HasMaxLength(500);
            
        builder.Property(m => m.ModeratedBy)
            .HasMaxLength(450);
            
        builder.Property(m => m.Priority)
            .HasConversion<int>()
            .HasDefaultValue(MessagePriority.Normal);
            
        builder.Property(m => m.Type)
            .HasConversion<int>()
            .HasDefaultValue(MessageType.Direct);
        
        // Indexes for performance
        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.RecipientId);
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => new { m.RecipientId, m.IsRead });
        builder.HasIndex(m => new { m.SenderId, m.RecipientId, m.CreatedAt });
        
        // Foreign key relationships
        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(m => m.Recipient)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(m => m.Rental)
            .WithMany()
            .HasForeignKey(m => m.RentalId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(m => m.Tool)
            .WithMany()
            .HasForeignKey(m => m.ToolId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}