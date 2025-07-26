using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IAuthenticationEventLogger
{
    /// <summary>
    /// Logs a successful login event
    /// </summary>
    Task LogLoginSuccessAsync(string? userId, string? userEmail, string ipAddress, string? userAgent = null, string? sessionId = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a failed login attempt
    /// </summary>
    Task LogLoginFailureAsync(string? userEmail, string ipAddress, string failureReason, string? userAgent = null, string? sessionId = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a password change event
    /// </summary>
    Task LogPasswordChangeAsync(string userId, string ipAddress, bool successful, string? userAgent = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs an account lockout event
    /// </summary>
    Task LogAccountLockoutAsync(string? userId, string? userEmail, string ipAddress, string reason, TimeSpan? lockoutDuration = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs an account unlock event
    /// </summary>
    Task LogAccountUnlockAsync(string? userId, string? userEmail, string ipAddress, string reason, string? unlockedBy = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a session creation event
    /// </summary>
    Task LogSessionCreatedAsync(string userId, string sessionId, string ipAddress, string? userAgent = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a session termination event
    /// </summary>
    Task LogSessionTerminatedAsync(string userId, string sessionId, string ipAddress, string reason, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a token blacklisting event
    /// </summary>
    Task LogTokenBlacklistedAsync(string tokenId, string? userId, string ipAddress, string reason, string? blacklistedBy = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs suspicious activity
    /// </summary>
    Task LogSuspiciousActivityAsync(string eventType, string? userId, string? userEmail, string ipAddress, decimal riskScore, string description, string? userAgent = null, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a brute force attempt
    /// </summary>
    Task LogBruteForceAttemptAsync(string attackType, string sourceIdentifier, string? targetIdentifier, string ipAddress, int attemptCount, decimal riskScore, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a geographic anomaly
    /// </summary>
    Task LogGeographicAnomalyAsync(string? userId, string? userEmail, string ipAddress, string currentLocation, string previousLocation, double travelDistance, double travelSpeed, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a session hijacking attempt
    /// </summary>
    Task LogSessionHijackingAttemptAsync(string userId, string sessionId, string ipAddress, string reason, decimal riskScore, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Logs a concurrent session violation
    /// </summary>
    Task LogConcurrentSessionViolationAsync(string userId, string ipAddress, int sessionCount, int maxAllowed, string action, Dictionary<string, object>? additionalData = null);
    
    /// <summary>
    /// Gets security events for a user
    /// </summary>
    Task<List<SecurityEvent>> GetUserSecurityEventsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
    
    /// <summary>
    /// Gets security events by type
    /// </summary>
    Task<List<SecurityEvent>> GetSecurityEventsByTypeAsync(string eventType, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
    
    /// <summary>
    /// Gets security events by IP address
    /// </summary>
    Task<List<SecurityEvent>> GetSecurityEventsByIpAsync(string ipAddress, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
    
    /// <summary>
    /// Gets high-risk security events
    /// </summary>
    Task<List<SecurityEvent>> GetHighRiskEventsAsync(decimal minRiskScore, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
    
    /// <summary>
    /// Sends real-time alerts for critical security events
    /// </summary>
    Task SendSecurityAlertAsync(SecurityEvent securityEvent);
    
    /// <summary>
    /// Gets security event statistics
    /// </summary>
    Task<SecurityEventStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Checks if event logging is enabled
    /// </summary>
    bool IsLoggingEnabled { get; }
}

public class SecurityEventStatistics
{
    public int TotalEvents { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public int SuspiciousActivities { get; set; }
    public int BruteForceAttempts { get; set; }
    public int GeographicAnomalies { get; set; }
    public int SessionHijackingAttempts { get; set; }
    public int AccountLockouts { get; set; }
    public int TokensBlacklisted { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsByCountry { get; set; } = new();
    public Dictionary<string, int> TopRiskyIPs { get; set; } = new();
    public Dictionary<string, int> EventsByHour { get; set; } = new();
    public decimal AverageRiskScore { get; set; }
    public decimal MaxRiskScore { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}