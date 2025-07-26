namespace ToolsSharing.Infrastructure.Models;

/// <summary>
/// Result of rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Identifier { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public int RequestCount { get; set; }
    public int RequestLimit { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public RateLimitViolationType? ViolationType { get; set; }
    public string? BlockReason { get; set; }
}

/// <summary>
/// Rate limit violation tracking
/// </summary>
public class RateLimitViolation
{
    public string Identifier { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public DateTime ViolationTime { get; set; }
    public int ViolationCount { get; set; }
    public TimeSpan BlockDuration { get; set; }
    public DateTime BlockedUntil { get; set; }
    public RateLimitViolationType ViolationType { get; set; }
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Rate limit tracking data
/// </summary>
public class RateLimitTracker
{
    public string Identifier { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public List<DateTime> RequestTimestamps { get; set; } = new();
    public DateTime FirstRequest { get; set; }
    public DateTime LastRequest { get; set; }
    public int TotalRequests { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public int ViolationCount { get; set; }
}

/// <summary>
/// Rate limit context for a request
/// </summary>
public class RateLimitContext
{
    public string IPAddress { get; set; } = "";
    public string? UserId { get; set; }
    public string? UserRole { get; set; }
    public string Endpoint { get; set; } = "";
    public string HttpMethod { get; set; } = "";
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Types of rate limit violations
/// </summary>
public enum RateLimitViolationType
{
    /// <summary>
    /// Standard rate limit exceeded
    /// </summary>
    RateLimitExceeded,
    
    /// <summary>
    /// Global IP limit exceeded
    /// </summary>
    GlobalIPLimitExceeded,
    
    /// <summary>
    /// Global user limit exceeded
    /// </summary>
    GlobalUserLimitExceeded,
    
    /// <summary>
    /// Anonymous user limit exceeded
    /// </summary>
    AnonymousLimitExceeded,
    
    /// <summary>
    /// Currently blocked due to previous violations
    /// </summary>
    CurrentlyBlocked,
    
    /// <summary>
    /// Suspicious rapid requests pattern
    /// </summary>
    SuspiciousActivity
}

/// <summary>
/// Rate limit headers to include in response
/// </summary>
public class RateLimitHeaders
{
    public const string Limit = "X-RateLimit-Limit";
    public const string Remaining = "X-RateLimit-Remaining";
    public const string Reset = "X-RateLimit-Reset";
    public const string RetryAfter = "Retry-After";
    public const string Policy = "X-RateLimit-Policy";
    
    public Dictionary<string, string> ToHeaderDictionary(RateLimitResult result)
    {
        var headers = new Dictionary<string, string>
        {
            [Limit] = result.RequestLimit.ToString(),
            [Remaining] = Math.Max(0, result.RequestLimit - result.RequestCount).ToString(),
            [Reset] = ((DateTimeOffset)result.WindowEnd).ToUnixTimeSeconds().ToString(),
            [Policy] = $"{result.RequestLimit};w={result.WindowDuration.TotalSeconds}"
        };
        
        if (result.RetryAfter.HasValue)
        {
            headers[RetryAfter] = result.RetryAfter.Value.TotalSeconds.ToString("0");
        }
        
        return headers;
    }
}