using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Models.EmailNotifications;

public class DisputeCreatedNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string InitiatorName { get; set; } = string.Empty;
    public string RentalToolName { get; set; } = string.Empty;
    public DateTime DisputeCreatedAt { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"New Dispute Created - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeCreated";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            InitiatorName,
            RentalToolName,
            DisputeCreatedAt,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeMessageNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
    public DateTime MessageCreatedAt { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"New Message in Dispute - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeMessage";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            SenderName,
            MessagePreview,
            MessageCreatedAt,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeStatusChangeNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"Dispute Status Updated - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeStatusChange";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            OldStatus,
            NewStatus,
            Notes,
            UpdatedAt,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeEscalationNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string EscalatedBy { get; set; } = string.Empty;
    public DateTime EscalatedAt { get; set; }
    public string? ExternalDisputeId { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"Dispute Escalated - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeEscalation";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            EscalatedBy,
            EscalatedAt,
            ExternalDisputeId,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeResolutionNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string ResolutionNotes { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"Dispute Resolved - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeResolution";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            Resolution,
            ResolutionNotes,
            ResolvedAt,
            RefundAmount,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeEvidenceNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"New Evidence Uploaded - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeEvidence";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            UploadedBy,
            FileCount,
            UploadedAt,
            DisputeUrl,
            RecipientName
        };
    }
}

public class DisputeOverdueNotification : EmailNotification
{
    public string DisputeId { get; set; } = string.Empty;
    public string DisputeTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public string DisputeUrl { get; set; } = string.Empty;

    public override string GetSubject()
    {
        return $"Dispute Response Overdue - {DisputeTitle}";
    }

    public override string GetTemplateName()
    {
        return "DisputeOverdue";
    }

    public override object GetTemplateData()
    {
        return new
        {
            DisputeId,
            DisputeTitle,
            DueDate,
            DaysOverdue,
            DisputeUrl,
            RecipientName
        };
    }
}