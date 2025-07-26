using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Models;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Redis-backed rate limiting service with sliding window algorithm
/// </summary>
public class RateLimitService : IRateLimitServiceExtended
{
    private readonly IDistributedCache _cache;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitService> _logger;
    
    // Cache key prefixes
    private const string RateLimitPrefix = "rl:";
    private const string ViolationPrefix = "rlv:";
    private const string BlockPrefix = "rlb:";
    
    public RateLimitService(
        IDistributedCache cache,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<RateLimitResult> CheckRateLimitAsync(RateLimitContext context)
    {
        if (!_options.EnableRateLimiting)
        {
            return new RateLimitResult { IsAllowed = true, Identifier = context.IPAddress };
        }
        
        // Check if currently blocked
        var blockInfo = await GetCurrentBlockAsync(context.IPAddress, context.Endpoint);
        if (blockInfo != null)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                Identifier = context.IPAddress,
                Endpoint = context.Endpoint,
                ViolationType = RateLimitViolationType.CurrentlyBlocked,
                RetryAfter = blockInfo.BlockedUntil - DateTime.UtcNow,
                BlockReason = $"Blocked due to {blockInfo.ViolationCount} violations"
            };
        }
        
        // Get endpoint configuration
        var endpointConfig = await GetEndpointConfigurationAsync(context.Endpoint);
        
        // Skip for admins if configured
        if (endpointConfig.SkipForAdmins && context.IsAdmin)
        {
            return new RateLimitResult { IsAllowed = true, Identifier = context.IPAddress };
        }
        
        // Check endpoint-specific rate limits
        var endpointResult = await CheckEndpointRateLimitAsync(context, endpointConfig);
        if (!endpointResult.IsAllowed)
        {
            await RecordViolationAsync(context, new RateLimitViolation
            {
                Identifier = context.IPAddress,
                Endpoint = context.Endpoint,
                ViolationTime = DateTime.UtcNow,
                ViolationType = RateLimitViolationType.RateLimitExceeded,
                ViolationCount = 1
            });
            return endpointResult;
        }
        
        // Check global rate limits
        var globalResult = await CheckGlobalRateLimitsAsync(context);
        if (!globalResult.IsAllowed)
        {
            await RecordViolationAsync(context, new RateLimitViolation
            {
                Identifier = context.IPAddress,
                Endpoint = "GLOBAL",
                ViolationTime = DateTime.UtcNow,
                ViolationType = globalResult.ViolationType ?? RateLimitViolationType.GlobalIPLimitExceeded,
                ViolationCount = 1
            });
            return globalResult;
        }
        
        return new RateLimitResult
        {
            IsAllowed = true,
            Identifier = context.IPAddress,
            Endpoint = context.Endpoint,
            RequestCount = endpointResult.RequestCount,
            RequestLimit = endpointResult.RequestLimit,
            WindowDuration = endpointConfig.WindowDuration,
            WindowStart = endpointResult.WindowStart,
            WindowEnd = endpointResult.WindowEnd
        };
    }
    
    private async Task<RateLimitResult> CheckEndpointRateLimitAsync(RateLimitContext context, EndpointRateLimit config)
    {
        var identifier = GetRateLimitIdentifier(context);
        var cacheKey = $"{RateLimitPrefix}{identifier}:{NormalizeEndpoint(context.Endpoint)}";
        
        var tracker = await GetOrCreateTrackerAsync(cacheKey, config.WindowDuration);
        
        // Clean expired timestamps using sliding window
        var windowStart = DateTime.UtcNow - config.WindowDuration;
        tracker.RequestTimestamps = tracker.RequestTimestamps
            .Where(ts => ts > windowStart)
            .ToList();
        
        var requestCount = tracker.RequestTimestamps.Count;
        var requestLimit = GetEffectiveRequestLimit(context, config);
        
        var result = new RateLimitResult
        {
            IsAllowed = requestCount < requestLimit,
            Identifier = identifier,
            Endpoint = context.Endpoint,
            RequestCount = requestCount,
            RequestLimit = requestLimit,
            WindowDuration = config.WindowDuration,
            WindowStart = windowStart,
            WindowEnd = DateTime.UtcNow,
            ViolationType = requestCount >= requestLimit ? RateLimitViolationType.RateLimitExceeded : null
        };
        
        if (!result.IsAllowed)
        {
            // Calculate retry after based on oldest request in window
            if (tracker.RequestTimestamps.Any())
            {
                var oldestRequest = tracker.RequestTimestamps.Min();
                result.RetryAfter = oldestRequest.Add(config.WindowDuration) - DateTime.UtcNow;
            }
        }
        
        return result;
    }
    
    private async Task<RateLimitResult> CheckGlobalRateLimitsAsync(RateLimitContext context)
    {
        var identifier = context.IPAddress;
        
        // Check hourly IP limit
        var hourlyKey = $"{RateLimitPrefix}global:ip:hour:{identifier}";
        var hourlyTracker = await GetOrCreateTrackerAsync(hourlyKey, TimeSpan.FromHours(1));
        
        var hourlyCount = CountRequestsInWindow(hourlyTracker, TimeSpan.FromHours(1));
        var hourlyLimit = context.IsAuthenticated ? 
            _options.GlobalLimits.RequestsPerHourPerUser : 
            _options.GlobalLimits.AnonymousRequestsPerHour;
        
        if (hourlyCount >= hourlyLimit)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                Identifier = identifier,
                Endpoint = "GLOBAL_HOURLY",
                RequestCount = hourlyCount,
                RequestLimit = hourlyLimit,
                WindowDuration = TimeSpan.FromHours(1),
                ViolationType = context.IsAuthenticated ? 
                    RateLimitViolationType.GlobalUserLimitExceeded : 
                    RateLimitViolationType.AnonymousLimitExceeded
            };
        }
        
        // Check daily IP limit
        var dailyKey = $"{RateLimitPrefix}global:ip:day:{identifier}";
        var dailyTracker = await GetOrCreateTrackerAsync(dailyKey, TimeSpan.FromDays(1));
        
        var dailyCount = CountRequestsInWindow(dailyTracker, TimeSpan.FromDays(1));
        var dailyLimit = _options.GlobalLimits.RequestsPerDayPerIP;
        
        if (dailyCount >= dailyLimit)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                Identifier = identifier,
                Endpoint = "GLOBAL_DAILY",
                RequestCount = dailyCount,
                RequestLimit = dailyLimit,
                WindowDuration = TimeSpan.FromDays(1),
                ViolationType = RateLimitViolationType.GlobalIPLimitExceeded
            };
        }
        
        return new RateLimitResult { IsAllowed = true, Identifier = identifier };
    }
    
    public async Task RecordRequestAsync(RateLimitContext context)
    {
        if (!_options.EnableRateLimiting) return;
        
        var tasks = new List<Task>();
        
        // Record endpoint-specific request
        var identifier = GetRateLimitIdentifier(context);
        var endpointKey = $"{RateLimitPrefix}{identifier}:{NormalizeEndpoint(context.Endpoint)}";
        tasks.Add(RecordRequestInTrackerAsync(endpointKey, context.RequestTime));
        
        // Record global IP request
        var globalKey = $"{RateLimitPrefix}global:ip:hour:{context.IPAddress}";
        tasks.Add(RecordRequestInTrackerAsync(globalKey, context.RequestTime));
        
        var dailyKey = $"{RateLimitPrefix}global:ip:day:{context.IPAddress}";
        tasks.Add(RecordRequestInTrackerAsync(dailyKey, context.RequestTime));
        
        await Task.WhenAll(tasks);
    }
    
    private async Task RecordRequestInTrackerAsync(string cacheKey, DateTime requestTime)
    {
        try
        {
            var trackerJson = await _cache.GetStringAsync(cacheKey);
            var tracker = string.IsNullOrEmpty(trackerJson) ? 
                new RateLimitTracker() : 
                JsonSerializer.Deserialize<RateLimitTracker>(trackerJson) ?? new RateLimitTracker();
            
            tracker.RequestTimestamps.Add(requestTime);
            tracker.LastRequest = requestTime;
            tracker.TotalRequests++;
            
            if (tracker.FirstRequest == default)
            {
                tracker.FirstRequest = requestTime;
            }
            
            var updatedJson = JsonSerializer.Serialize(tracker);
            await _cache.SetStringAsync(cacheKey, updatedJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record request in tracker for key {Key}", cacheKey);
        }
    }
    
    public async Task RecordViolationAsync(RateLimitContext context, RateLimitViolation violation)
    {
        try
        {
            var violationKey = $"{ViolationPrefix}{context.IPAddress}:{NormalizeEndpoint(context.Endpoint)}";
            var violationJson = JsonSerializer.Serialize(violation);
            
            await _cache.SetStringAsync(violationKey, violationJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.Penalties.ViolationTrackingWindow
            });
            
            // Calculate and apply penalty block
            await ApplyPenaltyBlockAsync(context, violation);
            
            _logger.LogWarning("Rate limit violation recorded: {Identifier} on {Endpoint}, Type: {ViolationType}", 
                violation.Identifier, violation.Endpoint, violation.ViolationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record rate limit violation for {Identifier}", context.IPAddress);
        }
    }
    
    private async Task ApplyPenaltyBlockAsync(RateLimitContext context, RateLimitViolation violation)
    {
        try
        {
            // Get violation history
            var historyKey = $"{ViolationPrefix}history:{context.IPAddress}";
            var historyJson = await _cache.GetStringAsync(historyKey);
            var violationCount = 1;
            
            if (!string.IsNullOrEmpty(historyJson))
            {
                var history = JsonSerializer.Deserialize<List<RateLimitViolation>>(historyJson) ?? new();
                // Count recent violations
                var recentViolations = history.Where(v => 
                    v.ViolationTime > DateTime.UtcNow - _options.Penalties.ViolationTrackingWindow).ToList();
                violationCount = recentViolations.Count + 1;
                
                // Update history
                history.Add(violation);
                history = history.Where(v => 
                    v.ViolationTime > DateTime.UtcNow - _options.Penalties.ViolationTrackingWindow).ToList();
                
                var updatedHistoryJson = JsonSerializer.Serialize(history);
                await _cache.SetStringAsync(historyKey, updatedHistoryJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.Penalties.ViolationTrackingWindow
                });
            }
            else
            {
                var newHistory = new List<RateLimitViolation> { violation };
                var historyJson2 = JsonSerializer.Serialize(newHistory);
                await _cache.SetStringAsync(historyKey, historyJson2, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.Penalties.ViolationTrackingWindow
                });
            }
            
            // Calculate block duration
            var blockDuration = CalculateBlockDuration(violationCount);
            var blockedUntil = DateTime.UtcNow.Add(blockDuration);
            
            // Apply block
            var blockInfo = new RateLimitViolation
            {
                Identifier = context.IPAddress,
                Endpoint = context.Endpoint,
                ViolationTime = DateTime.UtcNow,
                ViolationCount = violationCount,
                BlockDuration = blockDuration,
                BlockedUntil = blockedUntil,
                ViolationType = violation.ViolationType
            };
            
            var blockKey = $"{BlockPrefix}{context.IPAddress}:{NormalizeEndpoint(context.Endpoint)}";
            var blockJson = JsonSerializer.Serialize(blockInfo);
            
            await _cache.SetStringAsync(blockKey, blockJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = blockDuration
            });
            
            _logger.LogWarning("Applied penalty block: {Identifier} blocked for {Duration} (violation #{Count})", 
                context.IPAddress, blockDuration, violationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply penalty block for {Identifier}", context.IPAddress);
        }
    }
    
    private TimeSpan CalculateBlockDuration(int violationCount)
    {
        TimeSpan baseDuration = violationCount switch
        {
            1 => _options.Penalties.FirstViolationBlock,
            2 => _options.Penalties.SecondViolationBlock,
            _ => _options.Penalties.RepeatedViolationBlock
        };
        
        if (violationCount > 2)
        {
            var multiplier = Math.Pow(_options.Penalties.ViolationMultiplier, violationCount - 2);
            baseDuration = TimeSpan.FromTicks((long)(baseDuration.Ticks * multiplier));
        }
        
        return baseDuration > _options.Penalties.MaximumBlockDuration ? 
            _options.Penalties.MaximumBlockDuration : baseDuration;
    }
    
    public async Task<RateLimitViolation?> GetCurrentBlockAsync(string identifier, string endpoint)
    {
        try
        {
            var blockKey = $"{BlockPrefix}{identifier}:{NormalizeEndpoint(endpoint)}";
            var blockJson = await _cache.GetStringAsync(blockKey);
            
            if (string.IsNullOrEmpty(blockJson)) return null;
            
            var blockInfo = JsonSerializer.Deserialize<RateLimitViolation>(blockJson);
            
            if (blockInfo?.BlockedUntil > DateTime.UtcNow)
            {
                return blockInfo;
            }
            
            // Block has expired, clean it up
            await _cache.RemoveAsync(blockKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current block for {Identifier}", identifier);
            return null;
        }
    }
    
    public async Task<RateLimitTracker?> GetRateLimitStatusAsync(string identifier, string endpoint)
    {
        try
        {
            var cacheKey = $"{RateLimitPrefix}{identifier}:{NormalizeEndpoint(endpoint)}";
            var trackerJson = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(trackerJson)) return null;
            
            return JsonSerializer.Deserialize<RateLimitTracker>(trackerJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rate limit status for {Identifier}", identifier);
            return null;
        }
    }
    
    public async Task ResetRateLimitAsync(string identifier, string? endpoint = null)
    {
        try
        {
            var tasks = new List<Task>();
            
            if (endpoint != null)
            {
                // Reset specific endpoint
                var cacheKey = $"{RateLimitPrefix}{identifier}:{NormalizeEndpoint(endpoint)}";
                var blockKey = $"{BlockPrefix}{identifier}:{NormalizeEndpoint(endpoint)}";
                tasks.Add(_cache.RemoveAsync(cacheKey));
                tasks.Add(_cache.RemoveAsync(blockKey));
            }
            else
            {
                // Reset all endpoints for identifier (would need key pattern scanning in real implementation)
                // For now, reset known global keys
                tasks.Add(_cache.RemoveAsync($"{RateLimitPrefix}global:ip:hour:{identifier}"));
                tasks.Add(_cache.RemoveAsync($"{RateLimitPrefix}global:ip:day:{identifier}"));
                tasks.Add(_cache.RemoveAsync($"{ViolationPrefix}history:{identifier}"));
            }
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Rate limit reset for identifier {Identifier}, endpoint: {Endpoint}", 
                identifier, endpoint ?? "ALL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset rate limit for {Identifier}", identifier);
        }
    }
    
    public async Task<EndpointRateLimit> GetEndpointConfigurationAsync(string endpoint)
    {
        var normalizedEndpoint = NormalizeEndpoint(endpoint);
        
        // Try exact match first
        if (_options.EndpointPolicies.TryGetValue(normalizedEndpoint, out var exactConfig))
        {
            return exactConfig;
        }
        
        // Try pattern matching (longest match wins)
        var matchingPattern = _options.EndpointPolicies.Keys
            .Where(pattern => normalizedEndpoint.StartsWith(pattern.TrimEnd('*')))
            .OrderByDescending(pattern => pattern.Length)
            .FirstOrDefault();
        
        if (matchingPattern != null)
        {
            return _options.EndpointPolicies[matchingPattern];
        }
        
        // Return default configuration from options
        return _options.DefaultEndpointLimit;
    }
    
    public async Task CleanupExpiredDataAsync()
    {
        // In a real implementation, this would scan and clean up expired keys
        // For Redis, we rely on TTL for automatic cleanup
        _logger.LogDebug("Rate limit cleanup completed (TTL-based cleanup)");
        await Task.CompletedTask;
    }
    
    // Extended interface implementations
    public async Task<IEnumerable<RateLimitViolation>> GetRecentViolationsAsync(TimeSpan timeWindow)
    {
        // Implementation would scan violation keys and return recent violations
        // For brevity, returning empty collection
        await Task.CompletedTask;
        return Enumerable.Empty<RateLimitViolation>();
    }
    
    public async Task<IEnumerable<(string Identifier, int ViolationCount)>> GetTopViolatorsAsync(TimeSpan timeWindow, int limit = 10)
    {
        // Implementation would aggregate violation data
        await Task.CompletedTask;
        return Enumerable.Empty<(string, int)>();
    }
    
    public async Task<bool> IsSuspiciousActivityAsync(string identifier)
    {
        // Check for suspicious patterns like rapid consecutive requests
        try
        {
            var globalKey = $"{RateLimitPrefix}global:ip:hour:{identifier}";
            var tracker = await GetOrCreateTrackerAsync(globalKey, TimeSpan.FromHours(1));
            
            // Check for burst pattern (many requests in short time)
            var recentRequests = tracker.RequestTimestamps
                .Where(ts => ts > DateTime.UtcNow - TimeSpan.FromMinutes(1))
                .Count();
            
            return recentRequests > 50; // More than 50 requests per minute is suspicious
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check suspicious activity for {Identifier}", identifier);
            return false;
        }
    }
    
    // Helper methods
    private async Task<RateLimitTracker> GetOrCreateTrackerAsync(string cacheKey, TimeSpan expiration)
    {
        try
        {
            var trackerJson = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(trackerJson))
            {
                return JsonSerializer.Deserialize<RateLimitTracker>(trackerJson) ?? new RateLimitTracker();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize rate limit tracker for key {Key}", cacheKey);
        }
        
        return new RateLimitTracker();
    }
    
    private static string GetRateLimitIdentifier(RateLimitContext context)
    {
        // Use user ID for authenticated requests, IP for anonymous
        return context.IsAuthenticated && !string.IsNullOrEmpty(context.UserId) 
            ? $"user:{context.UserId}" 
            : $"ip:{context.IPAddress}";
    }
    
    private static string NormalizeEndpoint(string endpoint)
    {
        return endpoint.ToLowerInvariant().TrimEnd('/');
    }
    
    private static int CountRequestsInWindow(RateLimitTracker tracker, TimeSpan window)
    {
        var windowStart = DateTime.UtcNow - window;
        return tracker.RequestTimestamps.Count(ts => ts > windowStart);
    }
    
    private static int GetEffectiveRequestLimit(RateLimitContext context, EndpointRateLimit config)
    {
        // Use per-user limit if authenticated and specified
        if (context.IsAuthenticated && config.PerUserRequestsPerWindow.HasValue)
        {
            return config.PerUserRequestsPerWindow.Value;
        }
        
        return config.RequestsPerWindow;
    }
}