namespace ToolsSharing.Core.Common.Models;

/// <summary>
/// Email configuration settings for the application
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    
    // SMTP Configuration
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    
    // Sender Configuration
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "NeighborTools";
    public string ReplyToEmail { get; set; } = string.Empty;
    public string ReplyToName { get; set; } = string.Empty;
    
    // Email Provider Configuration
    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;
    public string ApiKey { get; set; } = string.Empty; // For SendGrid, Mailgun, etc.
    public string ApiSecret { get; set; } = string.Empty; // For providers that need it
    
    // Queue Configuration
    public bool EnableQueue { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 300; // 5 minutes
    public int BatchSize { get; set; } = 50;
    
    // Rate Limiting
    public int MaxEmailsPerHour { get; set; } = 1000;
    public int MaxEmailsPerDay { get; set; } = 10000;
    
    // Templates
    public string TemplatesPath { get; set; } = "EmailTemplates";
    public bool UseRazorTemplates { get; set; } = true;
    
    // Development/Testing
    public bool TestMode { get; set; } = false;
    public string TestEmailRecipient { get; set; } = string.Empty;
    public bool LogEmailContent { get; set; } = false;
    
    // URLs
    public string BaseUrl { get; set; } = string.Empty;
    public string UnsubscribeUrl { get; set; } = string.Empty;
    public string PrivacyPolicyUrl { get; set; } = string.Empty;
}

public enum EmailProvider
{
    Smtp,
    SendGrid,
    Mailgun,
    AmazonSes,
    Postmark,
    MailChimp
}