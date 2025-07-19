using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.DTOs.Messaging;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; }
    public bool IsModerated { get; set; }
    public string? ModerationReason { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? RentalToolName { get; set; }
    public string? ToolName { get; set; }
    public MessagePriority Priority { get; set; }
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
}

public class MessageSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string PreviewContent { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; }
    public Guid? ConversationId { get; set; }
    public MessagePriority Priority { get; set; }
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttachmentCount { get; set; }
}

public class MessageAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public bool IsScanned { get; set; }
    public bool IsSafe { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public string Id { get; set; } = string.Empty;
    public string Participant1Id { get; set; } = string.Empty;
    public string Participant1Name { get; set; } = string.Empty;
    public string Participant2Id { get; set; } = string.Empty;
    public string Participant2Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageContent { get; set; }
    public string? LastMessageSenderId { get; set; }
    public bool IsArchived { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? RentalToolName { get; set; }
    public string? ToolName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UnreadCount { get; set; }
    public int MessageCount { get; set; }
}

public class ConversationDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string Participant1Id { get; set; } = string.Empty;
    public string Participant1Name { get; set; } = string.Empty;
    public string Participant2Id { get; set; } = string.Empty;
    public string Participant2Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsArchived { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? RentalToolName { get; set; }
    public string? ToolName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public int UnreadCount { get; set; }
    public bool CanSendMessage { get; set; }
}

public class SendMessageRequest
{
    public string RecipientId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public MessageType Type { get; set; } = MessageType.Direct;
    public List<MessageAttachmentUpload> Attachments { get; set; } = new();
}

public class MessageAttachmentUpload
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
}

public class SendMessageResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MessageDto? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class MessageStatisticsDto
{
    public int TotalMessages { get; set; }
    public int UnreadMessages { get; set; }
    public int SentMessages { get; set; }
    public int ReceivedMessages { get; set; }
    public int ArchivedMessages { get; set; }
    public int ModeratedMessages { get; set; }
    public int BlockedMessages { get; set; }
    public int ConversationCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public Dictionary<MessageType, int> MessagesByType { get; set; } = new();
    public List<MessageTrendDto> MonthlyTrends { get; set; } = new();
}

public class MessageTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int MessageCount { get; set; }
    public int ConversationCount { get; set; }
}