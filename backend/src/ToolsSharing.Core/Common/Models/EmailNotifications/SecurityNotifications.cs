namespace ToolsSharing.Core.Common.Models.EmailNotifications;

public class LoginAlertNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public bool IsNewDevice { get; set; }
    public string SecuritySettingsUrl { get; set; } = string.Empty;
    
    public LoginAlertNotification()
    {
        Type = EmailNotificationType.LoginAlert;
        Priority = EmailPriority.High;
    }
    
    public override string GetSubject() => IsNewDevice 
        ? "New device login to your NeighborTools account" 
        : "Login to your NeighborTools account";
        
    public override string GetTemplateName() => "LoginAlert";
    public override object GetTemplateData() => new
    {
        UserName,
        IpAddress,
        Location,
        Device,
        Browser,
        LoginTime,
        IsNewDevice,
        SecuritySettingsUrl,
        Year = DateTime.UtcNow.Year
    };
}

public class TwoFactorCodeNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    
    public TwoFactorCodeNotification()
    {
        Type = EmailNotificationType.TwoFactorCode;
        Priority = EmailPriority.Critical;
    }
    
    public override string GetSubject() => "Your NeighborTools verification code";
    public override string GetTemplateName() => "TwoFactorCode";
    public override object GetTemplateData() => new
    {
        UserName,
        Code,
        ExpiresAt,
        ExpiresInMinutes = (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes,
        Year = DateTime.UtcNow.Year
    };
}

public class SecurityAlertNotification : EmailNotification
{
    public string UserName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
    public string SecuritySettingsUrl { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    
    public SecurityAlertNotification()
    {
        Type = EmailNotificationType.SecurityAlert;
        Priority = EmailPriority.Critical;
    }
    
    public override string GetSubject() => $"Security Alert: {AlertType}";
    public override string GetTemplateName() => "SecurityAlert";
    public override object GetTemplateData() => new
    {
        UserName,
        AlertType,
        AlertMessage,
        ActionRequired,
        SecuritySettingsUrl,
        OccurredAt,
        Year = DateTime.UtcNow.Year
    };
}