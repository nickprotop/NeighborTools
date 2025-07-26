namespace ToolsSharing.Infrastructure.Configuration;

public class AuthenticationLoggingOptions
{
    public const string SectionName = "AuthenticationLogging";

    public bool EnableAuthenticationLogging { get; set; } = true;
    public bool LogSuccessfulLogins { get; set; } = true;
    public bool LogFailedLogins { get; set; } = true;
    public bool LogPasswordChanges { get; set; } = true;
    public bool LogAccountLockouts { get; set; } = true;
    public bool LogSuspiciousActivity { get; set; } = true;
    public bool LogSessionEvents { get; set; } = true;
    public bool LogTokenEvents { get; set; } = true;
    public bool EnableRealTimeAlerts { get; set; } = true;
    public bool AlertOnBruteForce { get; set; } = true;
    public bool AlertOnGeographicAnomalies { get; set; } = true;
    public bool AlertOnSessionHijacking { get; set; } = true;
    public bool AlertOnUnknownDevices { get; set; } = true;
    public int AlertThresholdMinutes { get; set; } = 5;
    public int MaxAlertsPerHour { get; set; } = 10;
    public List<string> NotificationRecipients { get; set; } = new();
    public bool EnableSlackNotifications { get; set; } = false;
    public string? SlackWebhookUrl { get; set; }
    public bool EnableEmailNotifications { get; set; } = true;
    public List<string> EmailRecipients { get; set; } = new();
    public int LogRetentionDays { get; set; } = 90;
    public bool CompressOldLogs { get; set; } = true;
    public int CompressionThresholdDays { get; set; } = 30;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDashboard { get; set; } = true;
    public int DashboardRefreshSeconds { get; set; } = 30;
}