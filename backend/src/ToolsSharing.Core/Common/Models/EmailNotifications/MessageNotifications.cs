namespace ToolsSharing.Core.Common.Models.EmailNotifications;

public class NewMessageNotification : EmailNotification
{
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string MessageSubject { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
    public string MessageUrl { get; set; } = string.Empty;
    public string ConversationUrl { get; set; } = string.Empty;
    public string? RentalToolName { get; set; }
    public string? ToolName { get; set; }
    public bool HasAttachments { get; set; }
    public int AttachmentCount { get; set; }
    
    public NewMessageNotification()
    {
        Type = EmailNotificationType.MessageReceived;
        Priority = EmailPriority.Normal;
    }
    
    public override string GetSubject() => $"New message from {SenderName}: {MessageSubject}";
    public override string GetTemplateName() => "NewMessage";
    public override object GetTemplateData() => new
    {
        SenderName,
        SenderEmail,
        MessageSubject,
        MessagePreview = MessagePreview.Length > 150 ? MessagePreview.Substring(0, 150) + "..." : MessagePreview,
        MessageUrl,
        ConversationUrl,
        RentalToolName,
        ToolName,
        HasAttachments,
        AttachmentCount,
        Year = DateTime.UtcNow.Year
    };
}

public class MessageReplyNotification : EmailNotification
{
    public string SenderName { get; set; } = string.Empty;
    public string OriginalMessageSubject { get; set; } = string.Empty;
    public string ReplyContent { get; set; } = string.Empty;
    public string ConversationUrl { get; set; } = string.Empty;
    public string? RentalToolName { get; set; }
    public string? ToolName { get; set; }
    public bool HasAttachments { get; set; }
    
    public MessageReplyNotification()
    {
        Type = EmailNotificationType.MessageReceived;
        Priority = EmailPriority.Normal;
    }
    
    public override string GetSubject() => $"Reply from {SenderName}: Re: {OriginalMessageSubject}";
    public override string GetTemplateName() => "MessageReply";
    public override object GetTemplateData() => new
    {
        SenderName,
        OriginalMessageSubject,
        ReplyContent = ReplyContent.Length > 150 ? ReplyContent.Substring(0, 150) + "..." : ReplyContent,
        ConversationUrl,
        RentalToolName,
        ToolName,
        HasAttachments,
        Year = DateTime.UtcNow.Year
    };
}

public class MessageModerationNotification : EmailNotification
{
    public string MessageSubject { get; set; } = string.Empty;
    public string ModerationReason { get; set; } = string.Empty;
    public string ModeratedContent { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string AppealUrl { get; set; } = string.Empty;
    
    public MessageModerationNotification()
    {
        Type = EmailNotificationType.SecurityAlert;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => IsBlocked ? "Message blocked due to policy violation" : "Message modified due to content policy";
    public override string GetTemplateName() => "MessageModeration";
    public override object GetTemplateData() => new
    {
        MessageSubject,
        ModerationReason,
        ModeratedContent,
        OriginalContent,
        IsBlocked,
        AppealUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class ConversationDigestNotification : EmailNotification
{
    public string OtherParticipantName { get; set; } = string.Empty;
    public int UnreadMessageCount { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string ConversationUrl { get; set; } = string.Empty;
    public List<MessageSummary> RecentMessages { get; set; } = new();
    
    public ConversationDigestNotification()
    {
        Type = EmailNotificationType.MessageReceived;
        Priority = EmailPriority.Low;
    }
    
    public override string GetSubject() => $"You have {UnreadMessageCount} unread messages from {OtherParticipantName}";
    public override string GetTemplateName() => "ConversationDigest";
    public override object GetTemplateData() => new
    {
        OtherParticipantName,
        UnreadMessageCount,
        LastMessagePreview,
        LastMessageAt,
        ConversationUrl,
        RecentMessages,
        Year = DateTime.UtcNow.Year
    };
}

public class MessageSummary
{
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool HasAttachments { get; set; }
}