namespace ToolsSharing.Core.Common.Models.EmailNotifications;

/// <summary>
/// Simple payment email notification
/// </summary>
public class PaymentEmailNotification : EmailNotification
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PlainTextContent { get; set; }

    public PaymentEmailNotification()
    {
        Type = EmailNotificationType.PaymentProcessed;
    }

    public override string GetSubject() => Subject;
    
    public override string GetTemplateName() => "PaymentGeneric";
    
    public override object GetTemplateData()
    {
        return new
        {
            Subject,
            HtmlContent,
            PlainTextContent,
            RecipientName,
            Metadata
        };
    }
}

/// <summary>
/// Payment completion notification for tool owners
/// </summary>
public class PaymentCompletedNotification : EmailNotification
{
    public string OwnerName { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string RentalId { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public decimal? PlatformFee { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string RentalDetailsUrl { get; set; } = string.Empty;
    public string MessagesUrl { get; set; } = string.Empty;

    public PaymentCompletedNotification()
    {
        Type = EmailNotificationType.PaymentProcessed;
        Priority = EmailPriority.High;
    }

    public override string GetSubject() => $"Payment received for {ToolName} rental";
    
    public override string GetTemplateName() => "PaymentCompleted";
    
    public override object GetTemplateData()
    {
        return new
        {
            OwnerName,
            RenterName,
            ToolName,
            RentalId,
            PaidAmount,
            SecurityDeposit,
            PlatformFee,
            NetAmount,
            PaymentDate,
            RentalStartDate,
            RentalEndDate,
            PaymentMethod,
            RentalDetailsUrl,
            MessagesUrl,
            RecipientName,
            Metadata
        };
    }
}

/// <summary>
/// Helper class to create simple email notifications
/// </summary>
public static class SimpleEmailNotification
{
    public static PaymentEmailNotification Create(string recipientEmail, string subject, string htmlContent, EmailNotificationType type = EmailNotificationType.GeneralNotification)
    {
        return new PaymentEmailNotification
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            HtmlContent = htmlContent,
            PlainTextContent = htmlContent,
            Type = type
        };
    }
}