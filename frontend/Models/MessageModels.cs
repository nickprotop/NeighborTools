namespace frontend.Models;

public class MessageDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; }
    public bool IsModerated { get; set; }
    public bool IsBlocked { get; set; }
    public string? ModerationReason { get; set; }
    public string? ModerationSeverity { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public string? ModeratedBy { get; set; }
    public string? OriginalContent { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? ToolName { get; set; }
    public string? RentalToolName { get; set; }
    public MessagePriority Priority { get; set; }
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public string Participant1Id { get; set; } = "";
    public string Participant2Id { get; set; } = "";
    public string Participant1Name { get; set; } = "";
    public string Participant2Name { get; set; } = "";
    public string Title { get; set; } = "";
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? ToolName { get; set; }
    public Guid? LastMessageId { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public string? LastMessageContent { get; set; }
}

public class MessageAttachmentDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public string RecipientId { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public MessageType Type { get; set; } = MessageType.Direct;
    public List<FileUploadRequest> Attachments { get; set; } = new();
}

public class CreateConversationRequest
{
    public string ParticipantId { get; set; } = "";
    public string Title { get; set; } = "";
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
}

public class UpdateConversationRequest
{
    public string? Title { get; set; }
    public bool? IsArchived { get; set; }
}

public class GetMessagesRequest
{
    public string? SenderId { get; set; }
    public string? RecipientId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public bool? IsRead { get; set; }
    public bool? IsArchived { get; set; }
    public MessageType? Type { get; set; }
    public MessagePriority? Priority { get; set; }
    public string? SearchQuery { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class GetConversationsRequest
{
    public string? ParticipantId { get; set; }
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? SearchQuery { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class MarkMessageAsReadRequest
{
    public Guid MessageId { get; set; }
}

public class FileUploadRequest
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] FileData { get; set; } = Array.Empty<byte>();
}

public class ReplyToMessageRequest
{
    public string Content { get; set; } = "";
    public List<FileUploadRequest>? Attachments { get; set; }
}

public class MessageAttachmentRequest
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
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

public class MessageSummaryDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string PreviewContent { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; }
    public bool IsModerated { get; set; }
    public bool IsBlocked { get; set; }
    public string? ModerationReason { get; set; }
    public string? ModerationSeverity { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public string? ModeratedBy { get; set; }
    public string Content { get; set; } = "";
    public Guid? ConversationId { get; set; }
    public MessagePriority Priority { get; set; }
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttachmentCount { get; set; }
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
}

public class ConversationDetailsDto
{
    public Guid Id { get; set; }
    public string Participant1Id { get; set; } = "";
    public string Participant2Id { get; set; } = "";
    public string Participant1Name { get; set; } = "";
    public string Participant2Name { get; set; } = "";
    public string Title { get; set; } = "";
    public Guid? RentalId { get; set; }
    public Guid? ToolId { get; set; }
    public string? ToolName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public string? LastMessageContent { get; set; }
    public int UnreadCount { get; set; }
}

public class ModerationStatisticsDto
{
    public int TotalMessagesProcessed { get; set; }
    public int ApprovedMessages { get; set; }
    public int ModeratedMessages { get; set; }
    public int PendingReview { get; set; }
    public Dictionary<ModerationSeverity, int> ViolationsBySeverity { get; set; } = new();
    public List<string> CommonViolations { get; set; } = new();
}

public enum ModerationSeverity
{
    Clean = 0,
    Minor = 1,
    Moderate = 2,
    Severe = 3,
    Critical = 4
}

public class TopMessageSender
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public int MessageCount { get; set; }
    public int ViolationCount { get; set; }
    public bool IsActive { get; set; }
}

public class ViolationSummaryDto
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public int ViolationCount { get; set; }
    public string RiskLevel { get; set; } = "";
    public DateTime LastViolation { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsWarned { get; set; }
}

public class MessagingAnalyticsDto
{
    public int MessageGrowth { get; set; }
    public decimal GrowthPercentage { get; set; }
    public decimal ModerationRate { get; set; }
    public int ModeratedCount { get; set; }
    public int ActiveUsers { get; set; }
    public string AverageResponseTime { get; set; } = "";
    public List<ViolationPatternDto> TopViolations { get; set; } = new();
}

public class ViolationPatternDto
{
    public string Pattern { get; set; } = "";
    public int Count { get; set; }
    public string Severity { get; set; } = "";
}

