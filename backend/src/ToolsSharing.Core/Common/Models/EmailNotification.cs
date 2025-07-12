using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Models;

/// <summary>
/// Base class for all email notifications
/// </summary>
public abstract class EmailNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public EmailNotificationType Type { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? CorrelationId { get; set; }
    
    public abstract string GetSubject();
    public abstract string GetTemplateName();
    public abstract object GetTemplateData();
}

public enum EmailNotificationType
{
    // Account
    Welcome,
    EmailVerification,
    PasswordReset,
    PasswordChanged,
    AccountDeleted,
    
    // Security
    LoginAlert,
    TwoFactorCode,
    SecurityAlert,
    
    // Rentals
    RentalRequest,
    RentalApproved,
    RentalRejected,
    RentalCancelled,
    RentalReminder,
    RentalOverdue,
    RentalReturned,
    
    // Messages
    NewMessage,
    MessageDigest,
    
    // Reviews
    NewReview,
    ReviewResponse,
    
    // Marketing
    Newsletter,
    Promotion,
    ProductUpdate,
    
    // System
    SystemMaintenance,
    TermsUpdate,
    PrivacyUpdate
}

public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Email queue item for processing
/// </summary>
public class EmailQueueItem : BaseEntity
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public EmailNotificationType NotificationType { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum EmailStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Bounced,
    Spam,
    Unsubscribed,
    Cancelled
}

/// <summary>
/// Email tracking information
/// </summary>
public class EmailTracking : BaseEntity
{
    public string MessageId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public EmailNotificationType NotificationType { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public int OpenCount { get; set; } = 0;
    public DateTime? ClickedAt { get; set; }
    public int ClickCount { get; set; } = 0;
    public bool Unsubscribed { get; set; } = false;
    public DateTime? UnsubscribedAt { get; set; }
    public bool Bounced { get; set; } = false;
    public DateTime? BouncedAt { get; set; }
    public string? BounceReason { get; set; }
    public bool MarkedAsSpam { get; set; } = false;
    public DateTime? MarkedAsSpamAt { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}