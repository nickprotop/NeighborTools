using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.Models.SecurityAnalytics;

/// <summary>
/// Comprehensive security analytics dashboard data
/// </summary>
public class SecurityDashboardData
{
    public SecurityMetrics Metrics { get; set; } = new();
    public List<SecurityThreat> ActiveThreats { get; set; } = new();
    public List<SecurityAlert> RecentAlerts { get; set; } = new();
    public SecurityTrendData TrendData { get; set; } = new();
    public List<AttackPatternSummary> AttackPatterns { get; set; } = new();
    public List<GeographicThreat> GeographicThreats { get; set; } = new();
    public SystemHealthStatus SystemHealth { get; set; } = new();
}

/// <summary>
/// Key security metrics for dashboard overview
/// </summary>
public class SecurityMetrics
{
    public int TotalSecurityEvents { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int AccountLockouts { get; set; }
    public int SuspiciousActivities { get; set; }
    public int BruteForceAttempts { get; set; }
    public int GeographicAnomalies { get; set; }
    public int SessionHijackingAttempts { get; set; }
    public int TokensBlacklisted { get; set; }
    public int BlockedIPs { get; set; }
    public decimal AverageRiskScore { get; set; }
    public decimal MaxRiskScore { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
}

/// <summary>
/// Active security threat representation
/// </summary>
public class SecurityThreat
{
    public Guid Id { get; set; }
    public string ThreatType { get; set; } = string.Empty;
    public string SourceIP { get; set; } = string.Empty;
    public string? TargetUser { get; set; }
    public decimal RiskScore { get; set; }
    public ThreatSeverity Severity { get; set; }
    public ThreatStatus Status { get; set; }
    public DateTime FirstDetected { get; set; }
    public DateTime LastActivity { get; set; }
    public int EventCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public string? GeographicLocation { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
}

/// <summary>
/// Security alert for real-time notifications
/// </summary>
public class SecurityAlert
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public decimal RiskScore { get; set; }
    public Dictionary<string, object> AlertData { get; set; } = new();
    public List<string> SuggestedActions { get; set; } = new();
    public bool RequiresImmedateAction { get; set; }
}

/// <summary>
/// Security trend data for analytics charts
/// </summary>
public class SecurityTrendData
{
    public List<SecurityDataPoint> LoginAttempts { get; set; } = new();
    public List<SecurityDataPoint> FailedLogins { get; set; } = new();
    public List<SecurityDataPoint> SuspiciousActivity { get; set; } = new();
    public List<SecurityDataPoint> BruteForceAttacks { get; set; } = new();
    public List<SecurityDataPoint> RiskScoreTrend { get; set; } = new();
    public List<CountrySecurityData> CountryData { get; set; } = new();
    public List<HourlySecurityData> HourlyData { get; set; } = new();
    public List<TopAttackerData> TopAttackers { get; set; } = new();
}

/// <summary>
/// Individual data point for trend charts
/// </summary>
public class SecurityDataPoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Attack pattern summary for analytics
/// </summary>
public class AttackPatternSummary
{
    public string PatternType { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public decimal AverageRiskScore { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public List<string> CommonSources { get; set; } = new();
    public List<string> TargetedUsers { get; set; } = new();
    public AttackPatternTrend Trend { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, int> Characteristics { get; set; } = new();
}

/// <summary>
/// Geographic threat data for map visualization
/// </summary>
public class GeographicThreat
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int ThreatCount { get; set; }
    public decimal AverageRiskScore { get; set; }
    public decimal MaxRiskScore { get; set; }
    public ThreatSeverity Severity { get; set; }
    public List<string> ThreatTypes { get; set; } = new();
    public Dictionary<string, int> AttackBreakdown { get; set; } = new();
    public bool IsBlocked { get; set; }
}

/// <summary>
/// System health status for monitoring
/// </summary>
public class SystemHealthStatus
{
    public HealthStatus OverallHealth { get; set; }
    public List<HealthMetric> HealthMetrics { get; set; } = new();
    public List<SystemAlert> SystemAlerts { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
    public SecurityServiceStatus ServiceStatus { get; set; } = new();
    public PerformanceMetrics Performance { get; set; } = new();
}

/// <summary>
/// Country-specific security data
/// </summary>
public class CountrySecurityData
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int SuspiciousEvents { get; set; }
    public decimal RiskPercentage { get; set; }
    public List<string> TopThreatTypes { get; set; } = new();
}

/// <summary>
/// Hourly security activity data
/// </summary>
public class HourlySecurityData
{
    public int Hour { get; set; }
    public int TotalEvents { get; set; }
    public int SuspiciousEvents { get; set; }
    public int FailedLogins { get; set; }
    public int BruteForceAttempts { get; set; }
    public decimal AverageRiskScore { get; set; }
}

/// <summary>
/// Top attacker information
/// </summary>
public class TopAttackerData
{
    public string IPAddress { get; set; } = string.Empty;
    public string? GeographicLocation { get; set; }
    public int AttackCount { get; set; }
    public decimal TotalRiskScore { get; set; }
    public decimal AverageRiskScore { get; set; }
    public List<string> AttackTypes { get; set; } = new();
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}

/// <summary>
/// Individual health metric
/// </summary>
public class HealthMetric
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public DateTime LastChecked { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// System alert for operational issues
/// </summary>
public class SystemAlert
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Component { get; set; }
}

/// <summary>
/// Security service status monitoring
/// </summary>
public class SecurityServiceStatus
{
    public bool BruteForceProtectionActive { get; set; }
    public bool TokenBlacklistActive { get; set; }
    public bool SessionSecurityActive { get; set; }
    public bool GeolocationServiceActive { get; set; }
    public bool EmailNotificationsActive { get; set; }
    public bool SlackNotificationsActive { get; set; }
    public bool RateLimitingActive { get; set; }
    public bool IPBlockingActive { get; set; }
    public DateTime LastServiceCheck { get; set; }
    public Dictionary<string, string> ServiceErrors { get; set; } = new();
}

/// <summary>
/// Performance metrics for system monitoring
/// </summary>
public class PerformanceMetrics
{
    public double AverageResponseTime { get; set; }
    public double CacheHitRate { get; set; }
    public int ActiveConnections { get; set; }
    public int QueuedEvents { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public DateTime LastMeasured { get; set; }
}

/// <summary>
/// Security filter criteria for analytics queries
/// </summary>
public class SecurityAnalyticsFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> EventTypes { get; set; } = new();
    public List<string> IPAddresses { get; set; } = new();
    public List<string> UserIds { get; set; } = new();
    public List<string> Countries { get; set; } = new();
    public decimal? MinRiskScore { get; set; }
    public decimal? MaxRiskScore { get; set; }
    public ThreatSeverity? Severity { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
    public bool IncludeResolved { get; set; } = false;
}

/// <summary>
/// Security report generation request
/// </summary>
public class SecurityReportRequest
{
    [Required]
    public SecurityReportType ReportType { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required] 
    public DateTime EndDate { get; set; }
    
    public SecurityAnalyticsFilter? Filter { get; set; }
    public SecurityReportFormat Format { get; set; } = SecurityReportFormat.Json;
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetails { get; set; } = true;
    public string? Recipients { get; set; }
    public bool SendEmail { get; set; } = false;
}

/// <summary>
/// Generated security report data
/// </summary>
public class SecurityReport
{
    public Guid Id { get; set; }
    public SecurityReportType ReportType { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public SecurityDashboardData Data { get; set; } = new();
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
    public SecurityReportSummary Summary { get; set; } = new();
    public string? FilePath { get; set; }
    public SecurityReportFormat Format { get; set; }
}

/// <summary>
/// Security recommendation based on analysis
/// </summary>
public class SecurityRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
    public RecommendationCategory Category { get; set; }
    public List<string> ActionItems { get; set; } = new();
    public string? Impact { get; set; }
    public string? Effort { get; set; }
    public Dictionary<string, object> SupportingData { get; set; } = new();
}

/// <summary>
/// Security report summary
/// </summary>
public class SecurityReportSummary
{
    public int TotalEvents { get; set; }
    public int HighRiskEvents { get; set; }
    public int BlockedAttacks { get; set; }
    public int UniqueAttackers { get; set; }
    public decimal OverallRiskScore { get; set; }
    public ThreatSeverity OverallThreatLevel { get; set; }
    public List<string> KeyFindings { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    public Dictionary<string, int> EventBreakdown { get; set; } = new();
}

// Enums for security analytics

public enum ThreatSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ThreatStatus
{
    Active = 1,
    Monitoring = 2,
    Blocked = 3,
    Resolved = 4
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public enum AlertStatus
{
    New = 1,
    Acknowledged = 2,
    InProgress = 3,
    Resolved = 4,
    Ignored = 5
}

public enum AttackPatternTrend
{
    Decreasing = -1,
    Stable = 0,
    Increasing = 1
}

public enum HealthStatus
{
    Healthy = 1,
    Warning = 2,
    Critical = 3,
    Unknown = 4
}

public enum SortDirection
{
    Ascending = 1,
    Descending = 2
}

public enum SecurityReportType
{
    Dashboard = 1,
    ThreatAnalysis = 2,
    Compliance = 3,
    Performance = 4,
    Custom = 5
}

public enum SecurityReportFormat
{
    Json = 1,
    Pdf = 2,
    Excel = 3,
    Html = 4
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum RecommendationCategory
{
    Prevention = 1,
    Detection = 2,
    Response = 3,
    Recovery = 4,
    Configuration = 5,
    Monitoring = 6
}