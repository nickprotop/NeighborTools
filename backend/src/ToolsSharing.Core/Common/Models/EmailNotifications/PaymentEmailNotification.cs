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