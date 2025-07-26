using ToolsSharing.Core.Models.SecurityAnalytics;

namespace ToolsSharing.Infrastructure.Services;

public interface IPerformanceMetricsService
{
    /// <summary>
    /// Get real-time performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    
    /// <summary>
    /// Record a request response time
    /// </summary>
    void RecordResponseTime(double responseTimeMs);
    
    /// <summary>
    /// Record cache hit/miss
    /// </summary>
    void RecordCacheHit(bool hit);
    
    /// <summary>
    /// Get cache statistics from Redis
    /// </summary>
    Task<(double hitRate, long totalRequests)> GetCacheStatisticsAsync();
    
    /// <summary>
    /// Get system resource usage
    /// </summary>
    Task<(double cpuUsage, double memoryUsage)> GetSystemResourcesAsync();
    
    /// <summary>
    /// Get active database connections
    /// </summary>
    Task<int> GetActiveConnectionsAsync();
}