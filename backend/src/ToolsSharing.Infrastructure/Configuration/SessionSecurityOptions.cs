namespace ToolsSharing.Infrastructure.Configuration;

public class SessionSecurityOptions
{
    public const string SectionName = "SessionSecurity";

    public bool EnableSessionSecurity { get; set; } = true;
    public int MaxConcurrentSessions { get; set; } = 5;
    public bool EnableDeviceFingerprinting { get; set; } = true;
    public bool TerminateOnDeviceChange { get; set; } = false;
    public bool EnableGeographicValidation { get; set; } = true;
    public double MaxTravelSpeedKmh { get; set; } = 1000;
    public int MinLocationChangeIntervalMinutes { get; set; } = 30;
    public bool TerminateOnImpossibleTravel { get; set; } = true;
    public bool EnableHijackingDetection { get; set; } = true;
    public int MaxIpChangesPerSession { get; set; } = 3;
    public bool TerminateOnSuspectedHijacking { get; set; } = true;
    public bool EnableTokenRotation { get; set; } = true;
    public int TokenRotationIntervalMinutes { get; set; } = 15;
    public int TokenGracePeriodMinutes { get; set; } = 5;
    public bool EnableSessionCleanup { get; set; } = true;
    public int SessionCleanupIntervalHours { get; set; } = 24;
    public int ExpiredSessionRetentionDays { get; set; } = 30;
    public decimal SuspiciousSessionThreshold { get; set; } = 70m;
    public decimal TerminateSessionThreshold { get; set; } = 90m;
    public bool EnableRealTimeMonitoring { get; set; } = true;
    public bool AlertAdminsForHighRisk { get; set; } = true;
    public List<string> ExemptIpRanges { get; set; } = new();
    public List<string> SuspiciousUserAgents { get; set; } = new() { "curl", "wget", "python-requests", "PostmanRuntime" };
    public bool RequireReauthForSensitiveOps { get; set; } = true;
    public List<string> SensitiveEndpoints { get; set; } = new() { "/api/auth/change-password", "/api/auth/delete-account", "/api/payment/", "/api/admin/" };
    public int ReauthTimeoutMinutes { get; set; } = 5;
}