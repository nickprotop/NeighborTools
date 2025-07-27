namespace ToolsSharing.Infrastructure.Configuration;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";
    
    /// <summary>
    /// Enable/disable rate limiting globally
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;
    
    /// <summary>
    /// Default sliding window duration for rate limiting
    /// </summary>
    public TimeSpan SlidingWindowDuration { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Enable per-user rate limiting (requires authentication)
    /// </summary>
    public bool EnablePerUserLimiting { get; set; } = true;
    
    /// <summary>
    /// Enable per-IP rate limiting
    /// </summary>
    public bool EnablePerIPLimiting { get; set; } = true;
    
    /// <summary>
    /// Endpoint-specific rate limit policies
    /// </summary>
    public Dictionary<string, EndpointRateLimit> EndpointPolicies { get; set; } = new();
    
    /// <summary>
    /// Global rate limits applied to all endpoints
    /// </summary>
    public GlobalRateLimit GlobalLimits { get; set; } = new();
    
    /// <summary>
    /// Rate limit violation penalties
    /// </summary>
    public RateLimitPenalty Penalties { get; set; } = new();
    
    /// <summary>
    /// Default rate limit for endpoints that don't match any specific configuration
    /// </summary>
    public EndpointRateLimit DefaultEndpointLimit { get; set; } = new();
}

public class EndpointRateLimit
{
    /// <summary>
    /// Maximum requests per sliding window
    /// </summary>
    public int RequestsPerWindow { get; set; } = 2000;
    
    /// <summary>
    /// Sliding window duration for this endpoint
    /// </summary>
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Per-user limit (if different from per-IP)
    /// </summary>
    public int? PerUserRequestsPerWindow { get; set; }
    
    /// <summary>
    /// Priority level for this endpoint (higher = more lenient)
    /// </summary>
    public int Priority { get; set; } = 1;
    
    /// <summary>
    /// Skip rate limiting for admin users
    /// </summary>
    public bool SkipForAdmins { get; set; } = false;
}

public class GlobalRateLimit
{
    /// <summary>
    /// Global requests per hour per IP
    /// </summary>
    public int RequestsPerHourPerIP { get; set; } = 20000;
    
    /// <summary>
    /// Global requests per hour per authenticated user
    /// </summary>
    public int RequestsPerHourPerUser { get; set; } = 10000;
    
    /// <summary>
    /// Global requests per day per IP
    /// </summary>
    public int RequestsPerDayPerIP { get; set; } = 100000;
    
    /// <summary>
    /// Anonymous users (no authentication) limits
    /// </summary>
    public int AnonymousRequestsPerHour { get; set; } = 5000;
}

public class RateLimitPenalty
{
    /// <summary>
    /// Block duration for first rate limit violation
    /// </summary>
    public TimeSpan FirstViolationBlock { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Block duration for second violation (within penalty window)
    /// </summary>
    public TimeSpan SecondViolationBlock { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Block duration for repeated violations
    /// </summary>
    public TimeSpan RepeatedViolationBlock { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Time window to track violation history
    /// </summary>
    public TimeSpan ViolationTrackingWindow { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Multiplier for each subsequent violation
    /// </summary>
    public double ViolationMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Maximum block duration
    /// </summary>
    public TimeSpan MaximumBlockDuration { get; set; } = TimeSpan.FromHours(1);
}