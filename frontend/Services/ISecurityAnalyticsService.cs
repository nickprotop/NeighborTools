using ToolsSharing.Core.Models.SecurityAnalytics;

namespace ToolsSharing.Frontend.Services;

/// <summary>
/// Frontend service interface for security analytics
/// </summary>
public interface ISecurityAnalyticsService
{
    // Dashboard and Overview
    Task<SecurityDashboardData> GetSecurityDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<SystemHealthStatus> GetSystemHealthAsync();
    
    // Threat Detection and Analysis
    Task<List<SecurityThreat>> GetActiveThreatsAsync(int limit = 50);
    Task<SecurityThreat?> AnalyzeThreatAsync(string sourceIP, string? userId = null);
    
    // Alert Management
    Task<List<SecurityAlert>> GetActiveAlertsAsync(int limit = 100);
    
    // Trend Analysis
    Task<SecurityTrendData> GetSecurityTrendsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<AttackPatternSummary>> GetAttackPatternsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeographicThreat>> GetGeographicThreatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    // Risk Assessment
    Task<decimal> CalculateRiskScoreAsync(string sourceIP, string? userId = null, Dictionary<string, object>? factors = null);
    Task<bool> IsIPHighRiskAsync(string ipAddress);
}