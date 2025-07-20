using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Models.EmailNotifications;

/// <summary>
/// Notification sent when a mutual closure request is created
/// </summary>
public class MutualClosureRequestNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public decimal? AgreedRefundAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int HoursToRespond { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"📝 Mutual Resolution Proposed for Dispute: {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureRequest";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            ProposedResolution,
            AgreedRefundAmount,
            ExpiresAt,
            HoursToRespond,
            DisputeUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent when a mutual closure request is responded to (accepted or rejected)
/// </summary>
public class MutualClosureResponseNotification : EmailNotification
{
    public string RespondingUserName { get; set; } = string.Empty;
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public bool WasAccepted { get; set; }
    public string ResponseMessage { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return WasAccepted 
            ? $"✅ Mutual Resolution Accepted for: {DisputeTitle}"
            : $"❌ Mutual Resolution Rejected for: {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureResponse";
    }

    public override object GetTemplateData()
    {
        return new
        {
            RespondingUserName,
            InitiatedByUserName,
            DisputeTitle,
            ProposedResolution,
            WasAccepted,
            ResponseMessage,
            RejectionReason,
            RefundAmount,
            DisputeUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent when a mutual closure request is about to expire
/// </summary>
public class MutualClosureExpiryReminderNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int HoursRemaining { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"⏰ Mutual Resolution Expires Soon: {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureExpiryReminder";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            ProposedResolution,
            ExpiresAt,
            HoursRemaining,
            DisputeUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent when a mutual closure request has expired
/// </summary>
public class MutualClosureExpiredNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"⏰ Mutual Resolution Expired: {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureExpired";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            ProposedResolution,
            ExpiredAt,
            DisputeUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent when a mutual closure request is cancelled by the initiator
/// </summary>
public class MutualClosureCancelledNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"🚫 Mutual Resolution Cancelled: {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureCancelled";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            ProposedResolution,
            CancellationReason,
            DisputeUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent to admins when a mutual closure request requires review
/// </summary>
public class MutualClosureAdminReviewNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string ProposedResolution { get; set; } = string.Empty;
    public decimal? AgreedRefundAmount { get; set; }
    public string ReviewReason { get; set; } = string.Empty;
    public new DateTime CreatedAt { get; set; }
    public string AdminPanelUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"🔍 Admin Review Required: Mutual Resolution for {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureAdminReview";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            ProposedResolution,
            AgreedRefundAmount,
            ReviewReason,
            CreatedAt,
            AdminPanelUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent to admins for high-value mutual closure requests
/// </summary>
public class MutualClosureHighValueAlertNotification : EmailNotification
{
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string ProposedResolution { get; set; } = string.Empty;
    public new DateTime CreatedAt { get; set; }
    public string AdminPanelUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"💰 High-Value Mutual Resolution Alert: {RefundAmount:C} - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureHighValueAlert";
    }

    public override object GetTemplateData()
    {
        return new
        {
            InitiatedByUserName,
            ResponseRequiredFromUserName,
            DisputeTitle,
            RefundAmount,
            ProposedResolution,
            CreatedAt,
            AdminPanelUrl,
            RecipientName
        };
    }
}

/// <summary>
/// Notification sent to admins for suspicious mutual closure activity
/// </summary>
public class MutualClosureSuspiciousActivityNotification : EmailNotification
{
    public new string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string SuspiciousActivity { get; set; } = string.Empty;
    public string ActivityDetails { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public int RecentRequestCount { get; set; }
    public string AdminPanelUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"🚨 Suspicious Mutual Closure Activity: {UserName}";
    }

    public override string GetTemplateName()
    {
        return "MutualClosureSuspiciousActivity";
    }

    public override object GetTemplateData()
    {
        return new
        {
            UserId,
            UserName,
            SuspiciousActivity,
            ActivityDetails,
            DetectedAt,
            RecentRequestCount,
            AdminPanelUrl,
            RecipientName
        };
    }
}