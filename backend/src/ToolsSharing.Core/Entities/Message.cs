namespace ToolsSharing.Core.Entities;

public class Message : BaseEntity
{
    public string SenderId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? OriginalContent { get; set; } // Store original before moderation
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; } = false;
    public bool IsModerated { get; set; } = false;
    public string? ModerationReason { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public string? ModeratedBy { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; } // Optional link to rental context
    public Guid? ToolId { get; set; } // Optional link to tool context
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public MessageType Type { get; set; } = MessageType.Direct;
    
    // Navigation properties
    public User Sender { get; set; } = null!;
    public User Recipient { get; set; } = null!;
    public Conversation? Conversation { get; set; }
    public Rental? Rental { get; set; }
    public Tool? Tool { get; set; }
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}

public enum MessagePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum MessageType
{
    Direct = 1,
    System = 2,
    RentalRelated = 3,
    ToolInquiry = 4,
    SupportRequest = 5
}