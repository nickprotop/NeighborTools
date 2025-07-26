using ToolsSharing.Core.Models.SecurityAnalytics;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Service for security analytics, threat detection, and reporting
/// </summary>
public interface ISecurityAnalyticsService
{
    // Dashboard and Overview
    Task<SecurityDashboardData> GetSecurityDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<SystemHealthStatus> GetSystemHealthAsync();
    
    // Threat Detection and Analysis
    Task<List<SecurityThreat>> GetActiveThreatsAsync(int limit = 50);
    Task<List<SecurityThreat>> GetThreatsByTypeAsync(string threatType, DateTime? startDate = null, DateTime? endDate = null);
    Task<SecurityThreat?> GetThreatByIdAsync(Guid threatId);
    Task<SecurityThreat?> AnalyzeThreatAsync(string sourceIP, string? userId = null);
    Task UpdateThreatStatusAsync(Guid threatId, ThreatStatus status, string? updatedBy = null);
    Task<bool> BlockThreatAsync(Guid threatId, TimeSpan? duration = null, string? reason = null);
    
    // Alert Management
    Task<List<SecurityAlert>> GetActiveAlertsAsync(int limit = 100);
    Task<List<SecurityAlert>> GetAlertsByTypeAsync(string alertType, DateTime? startDate = null, DateTime? endDate = null);
    Task<SecurityAlert?> GetAlertByIdAsync(Guid alertId);
    Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy);
    Task ResolveAlertAsync(Guid alertId, string resolvedBy);
    Task CreateSecurityAlertAsync(string alertType, string message, AlertSeverity severity, string sourceIP, 
        string? userId = null, decimal riskScore = 0, Dictionary<string, object>? alertData = null);
    
    // Trend Analysis
    Task<SecurityTrendData> GetSecurityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<SecurityDataPoint>> GetTrendDataAsync(string metricType, DateTime startDate, DateTime endDate, 
        string interval = "hour");
    Task<List<AttackPatternSummary>> GetAttackPatternsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeographicThreat>> GetGeographicThreatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    // Advanced Analytics
    Task<List<TopAttackerData>> GetTopAttackersAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<CountrySecurityData>> GetCountrySecurityDataAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<HourlySecurityData>> GetHourlySecurityDataAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> CalculateRiskScoreAsync(string sourceIP, string? userId = null, Dictionary<string, object>? factors = null);
    Task<bool> IsIPHighRiskAsync(string ipAddress);
    
    // Search and Filtering
    Task<List<SecurityThreat>> SearchThreatsAsync(SecurityAnalyticsFilter filter);
    Task<List<SecurityAlert>> SearchAlertsAsync(SecurityAnalyticsFilter filter);
    Task<SecurityDashboardData> GetFilteredDashboardAsync(SecurityAnalyticsFilter filter);
    
    // Reporting
    Task<SecurityReport> GenerateReportAsync(SecurityReportRequest request);
    Task<List<SecurityRecommendation>> GetSecurityRecommendationsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportReportAsync(Guid reportId, SecurityReportFormat format);
    Task<bool> ScheduleReportAsync(SecurityReportRequest request, string cronExpression, string? recipients = null);
    
    // Real-time Monitoring
    Task<bool> StartRealTimeMonitoringAsync();
    Task<bool> StopRealTimeMonitoringAsync();
    Task<List<SecurityAlert>> GetRealtimeAlertsAsync(DateTime since);
    Task NotifySecurityEventAsync(string eventType, string sourceIP, string? userId = null, 
        decimal riskScore = 0, Dictionary<string, object>? eventData = null);
    
    // Configuration and Maintenance
    Task<bool> UpdateThreatDetectionRulesAsync(Dictionary<string, object> rules);
    Task<bool> UpdateAlertThresholdsAsync(Dictionary<string, decimal> thresholds);
    Task<bool> CleanupOldDataAsync(TimeSpan retentionPeriod);
    Task<bool> RecalculateRiskScoresAsync(DateTime? startDate = null);
    
    // Statistics and Metrics
    Task<Dictionary<string, int>> GetEventStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Dictionary<string, decimal>> GetRiskStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Dictionary<string, object>> GetPerformanceMetricsAsync();
    Task<bool> ValidateSystemIntegrityAsync();
}