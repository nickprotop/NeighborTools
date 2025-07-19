using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Participant1Id)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(c => c.Participant2Id)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(c => c.Title)
            .HasMaxLength(200);
        
        // Indexes for performance
        builder.HasIndex(c => c.Participant1Id);
        builder.HasIndex(c => c.Participant2Id);
        builder.HasIndex(c => new { c.Participant1Id, c.Participant2Id }).IsUnique();
        builder.HasIndex(c => c.LastMessageAt);
        builder.HasIndex(c => c.RentalId);
        builder.HasIndex(c => c.ToolId);
        
        // Foreign key relationships
        builder.HasOne(c => c.Participant1)
            .WithMany(u => u.ConversationsAsParticipant1)
            .HasForeignKey(c => c.Participant1Id)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.Participant2)
            .WithMany(u => u.ConversationsAsParticipant2)
            .HasForeignKey(c => c.Participant2Id)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.LastMessage)
            .WithMany()
            .HasForeignKey(c => c.LastMessageId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(c => c.Rental)
            .WithMany()
            .HasForeignKey(c => c.RentalId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(c => c.Tool)
            .WithMany()
            .HasForeignKey(c => c.ToolId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}