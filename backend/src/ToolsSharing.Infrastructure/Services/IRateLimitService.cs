using ToolsSharing.Infrastructure.Models;
using ToolsSharing.Infrastructure.Configuration;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Service for managing rate limiting with sliding window algorithm
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Check if a request is allowed under rate limiting rules
    /// </summary>
    /// <param name="context">Rate limit context containing request information</param>
    /// <returns>Rate limit result with decision and metadata</returns>
    Task<RateLimitResult> CheckRateLimitAsync(RateLimitContext context);
    
    /// <summary>
    /// Record a successful request (after it passes rate limiting)
    /// </summary>
    /// <param name="context">Rate limit context</param>
    Task RecordRequestAsync(RateLimitContext context);
    
    /// <summary>
    /// Record a rate limit violation
    /// </summary>
    /// <param name="context">Rate limit context</param>
    /// <param name="violation">Violation details</param>
    Task RecordViolationAsync(RateLimitContext context, RateLimitViolation violation);
    
    /// <summary>
    /// Check if an identifier is currently blocked
    /// </summary>
    /// <param name="identifier">IP address or user ID</param>
    /// <param name="endpoint">Endpoint being accessed</param>
    /// <returns>Block information if blocked, null if not blocked</returns>
    Task<RateLimitViolation?> GetCurrentBlockAsync(string identifier, string endpoint);
    
    /// <summary>
    /// Get rate limit statistics for an identifier
    /// </summary>
    /// <param name="identifier">IP address or user ID</param>
    /// <param name="endpoint">Endpoint to check</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitTracker?> GetRateLimitStatusAsync(string identifier, string endpoint);
    
    /// <summary>
    /// Reset rate limits for an identifier (admin function)
    /// </summary>
    /// <param name="identifier">IP address or user ID to reset</param>
    /// <param name="endpoint">Specific endpoint or null for all endpoints</param>
    Task ResetRateLimitAsync(string identifier, string? endpoint = null);
    
    /// <summary>
    /// Get rate limit configuration for an endpoint
    /// </summary>
    /// <param name="endpoint">Endpoint path</param>
    /// <returns>Rate limit configuration</returns>
    Task<EndpointRateLimit> GetEndpointConfigurationAsync(string endpoint);
    
    /// <summary>
    /// Clean up expired rate limit data
    /// </summary>
    Task CleanupExpiredDataAsync();
}

/// <summary>
/// Extension of IRateLimitService for additional functionality
/// </summary>
public interface IRateLimitServiceExtended : IRateLimitService
{
    /// <summary>
    /// Get current violations for monitoring
    /// </summary>
    /// <param name="timeWindow">Time window to check</param>
    /// <returns>List of current violations</returns>
    Task<IEnumerable<RateLimitViolation>> GetRecentViolationsAsync(TimeSpan timeWindow);
    
    /// <summary>
    /// Get top rate limited identifiers
    /// </summary>
    /// <param name="timeWindow">Time window to analyze</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Top rate limited identifiers with counts</returns>
    Task<IEnumerable<(string Identifier, int ViolationCount)>> GetTopViolatorsAsync(TimeSpan timeWindow, int limit = 10);
    
    /// <summary>
    /// Check if identifier shows suspicious activity patterns
    /// </summary>
    /// <param name="identifier">Identifier to check</param>
    /// <returns>True if suspicious patterns detected</returns>
    Task<bool> IsSuspiciousActivityAsync(string identifier);
}