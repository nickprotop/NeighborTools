namespace ToolsSharing.Core.Entities;

public class Conversation : BaseEntity
{
    public string Participant1Id { get; set; } = string.Empty;
    public string Participant2Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public Guid? LastMessageId { get; set; }
    public bool IsArchived { get; set; } = false;
    public Guid? RentalId { get; set; } // Optional rental context
    public Guid? ToolId { get; set; } // Optional tool context
    
    // Navigation properties
    public User Participant1 { get; set; } = null!;
    public User Participant2 { get; set; } = null!;
    public Message? LastMessage { get; set; }
    public Rental? Rental { get; set; }
    public Tool? Tool { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}