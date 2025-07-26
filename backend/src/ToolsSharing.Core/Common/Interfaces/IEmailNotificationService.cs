using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;

namespace ToolsSharing.Core.Common.Interfaces;

/// <summary>
/// Advanced email notification service interface
/// </summary>
public interface IEmailNotificationService
{
    // Send notifications
    Task<bool> SendNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task<bool> SendNotificationAsync<T>(T notification, CancellationToken cancellationToken = default) where T : EmailNotification;
    Task<int> SendBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default);
    
    // Queue management
    Task<Guid> QueueNotificationAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task<List<Guid>> QueueBatchNotificationsAsync(IEnumerable<EmailNotification> notifications, CancellationToken cancellationToken = default);
    Task<bool> CancelQueuedNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    // Template management
    Task<string> RenderTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default);
    Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default);
    
    // User preferences
    Task<bool> CanSendToUserAsync(string userId, EmailNotificationType type, CancellationToken cancellationToken = default);
    Task<bool> IsUserUnsubscribedAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default);
    Task UnsubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default);
    Task ResubscribeUserAsync(string email, EmailNotificationType? type = null, CancellationToken cancellationToken = default);
    
    // Tracking
    Task TrackEmailOpenedAsync(string messageId, CancellationToken cancellationToken = default);
    Task TrackEmailClickedAsync(string messageId, string link, CancellationToken cancellationToken = default);
    Task<EmailTracking?> GetEmailTrackingAsync(string messageId, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<EmailStatistics> GetStatisticsAsync(DateTime from, DateTime to, EmailNotificationType? type = null, CancellationToken cancellationToken = default);
    Task<List<EmailQueueItem>> GetFailedEmailsAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<bool> RetryFailedEmailAsync(Guid queueItemId, CancellationToken cancellationToken = default);
    
    // Security alerts
    Task<bool> SendSecurityAlertAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default);
    
    // Additional methods for API endpoints
    Task<Dictionary<string, bool>> GetUserNotificationPreferencesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserNotificationPreferencesAsync(string userId, Dictionary<string, bool> preferences, CancellationToken cancellationToken = default);
    Task<bool> UnsubscribeUserAsync(string email, string token, CancellationToken cancellationToken = default);
    Task<EmailStatistics> GetEmailStatisticsAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> PreviewEmailTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default);
    Task<QueueStatus> GetQueueStatusAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessQueueManuallyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Email queue processor interface
/// </summary>
public interface IEmailQueueProcessor
{
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ProcessItemAsync(EmailQueueItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Email template engine interface
/// </summary>
public interface IEmailTemplateEngine
{
    Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default);
    Task<(string html, string plainText)> RenderWithPlainTextAsync(string templateName, object model, CancellationToken cancellationToken = default);
    Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default);
    void ClearCache();
}

/// <summary>
/// Email provider interface for different email services
/// </summary>
public interface IEmailProvider
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<List<EmailSendResult>> SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
    EmailProvider ProviderType { get; }
    bool IsConfigured { get; }
}

/// <summary>
/// Email message model
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
    public string? ReplyToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public List<EmailAttachment> Attachments { get; set; } = new();
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public string? MessageId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Email attachment model
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}

/// <summary>
/// Email send result
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public EmailProvider Provider { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ProviderResponse { get; set; } = new();
}

/// <summary>
/// Email statistics model
/// </summary>
public class EmailStatistics
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalUnsubscribed { get; set; }
    public int TotalBounced { get; set; }
    public int TotalSpam { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
    public double BounceRate { get; set; }
    public Dictionary<EmailNotificationType, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByDay { get; set; } = new();
}

/// <summary>
/// Email queue status model
/// </summary>
public class QueueStatus
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int FailedCount { get; set; }
    public bool IsProcessorRunning { get; set; }
    public DateTime? LastProcessedDate { get; set; }
}