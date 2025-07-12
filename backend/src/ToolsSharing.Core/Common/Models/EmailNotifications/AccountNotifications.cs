namespace ToolsSharing.Core.Common.Models.EmailNotifications;

public class WelcomeEmailNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string ActivationUrl { get; set; } = string.Empty;
    
    public WelcomeEmailNotification()
    {
        Type = EmailNotificationType.Welcome;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => "Welcome to NeighborTools!";
    public override string GetTemplateName() => "Welcome";
    public override object GetTemplateData() => new
    {
        UserName,
        ActivationUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class EmailVerificationNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    
    public EmailVerificationNotification()
    {
        Type = EmailNotificationType.EmailVerification;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => "Verify your email address";
    public override string GetTemplateName() => "EmailVerification";
    public override object GetTemplateData() => new
    {
        UserName,
        VerificationUrl,
        VerificationToken,
        Year = DateTime.UtcNow.Year
    };
}

public class PasswordResetNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string ResetUrl { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    
    public PasswordResetNotification()
    {
        Type = EmailNotificationType.PasswordReset;
        Priority = EmailPriority.Critical;
    }
    
    public override string GetSubject() => "Reset Your NeighborTools Password";
    public override string GetTemplateName() => "PasswordReset";
    public override object GetTemplateData() => new
    {
        UserName,
        ResetUrl,
        ResetToken,
        ExpiresAt,
        ExpiresInHours = (int)(ExpiresAt - DateTime.UtcNow).TotalHours,
        Year = DateTime.UtcNow.Year
    };
}

public class PasswordChangedNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    
    public PasswordChangedNotification()
    {
        Type = EmailNotificationType.PasswordChanged;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => "Your password has been changed";
    public override string GetTemplateName() => "PasswordChanged";
    public override object GetTemplateData() => new
    {
        UserName,
        IpAddress,
        UserAgent,
        ChangedAt,
        Year = DateTime.UtcNow.Year
    };
}