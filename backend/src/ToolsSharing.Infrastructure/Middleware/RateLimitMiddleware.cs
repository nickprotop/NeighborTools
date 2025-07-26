using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ToolsSharing.Infrastructure.Models;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Infrastructure.Middleware;

/// <summary>
/// Middleware for rate limiting requests with sliding window algorithm
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitMiddleware> _logger;
    
    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for certain paths
        if (ShouldSkipRateLimit(context))
        {
            await _next(context);
            return;
        }
        
        try
        {
            // Build rate limit context
            var rateLimitContext = BuildRateLimitContext(context);
            
            // Check rate limits
            var result = await _rateLimitService.CheckRateLimitAsync(rateLimitContext);
            
            // Add rate limit headers to response
            AddRateLimitHeaders(context, result);
            
            if (!result.IsAllowed)
            {
                // Request is rate limited
                await HandleRateLimitExceededAsync(context, result);
                return;
            }
            
            // Request is allowed - continue processing
            await _next(context);
            
            // Record successful request after processing
            await _rateLimitService.RecordRequestAsync(rateLimitContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limit middleware for request {Path}", context.Request.Path);
            
            // On error, allow request to continue (fail open)
            await _next(context);
        }
    }
    
    private static bool ShouldSkipRateLimit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Skip health checks, metrics, and other internal endpoints
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/ready",
            "/alive",
            "/.well-known",
            "/favicon.ico"
        };
        
        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }
    
    private static RateLimitContext BuildRateLimitContext(HttpContext context)
    {
        var user = context.User;
        var ipAddress = GetClientIPAddress(context);
        
        return new RateLimitContext
        {
            IPAddress = ipAddress,
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserRole = user.FindFirst(ClaimTypes.Role)?.Value,
            Endpoint = NormalizeEndpoint(context.Request.Path.Value ?? ""),
            HttpMethod = context.Request.Method,
            IsAuthenticated = user.Identity?.IsAuthenticated == true,
            IsAdmin = user.IsInRole("Admin"),
            RequestTime = DateTime.UtcNow,
            Headers = ExtractRelevantHeaders(context)
        };
    }
    
    private static string GetClientIPAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header first (proxy scenarios)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // Take the first IP if multiple are present
            var firstIp = xForwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
            {
                return firstIp;
            }
        }
        
        // Check for X-Real-IP header
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp) && IPAddress.TryParse(xRealIp, out _))
        {
            return xRealIp;
        }
        
        // Check for CF-Connecting-IP (Cloudflare)
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfConnectingIp) && IPAddress.TryParse(cfConnectingIp, out _))
        {
            return cfConnectingIp;
        }
        
        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
    
    private static string NormalizeEndpoint(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        
        // Remove query parameters
        var questionMarkIndex = path.IndexOf('?');
        if (questionMarkIndex >= 0)
        {
            path = path.Substring(0, questionMarkIndex);
        }
        
        // Normalize to lowercase and remove trailing slash
        path = path.ToLowerInvariant().TrimEnd('/');
        
        // Replace dynamic segments with patterns for grouping
        // Example: /api/users/123 -> /api/users/*
        if (path.StartsWith("/api/"))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var normalizedSegments = new List<string>();
            
            foreach (var segment in segments)
            {
                // Check if segment looks like an ID (GUID, number, etc.)
                if (IsIdSegment(segment))
                {
                    normalizedSegments.Add("*");
                }
                else
                {
                    normalizedSegments.Add(segment);
                }
            }
            
            return "/" + string.Join("/", normalizedSegments);
        }
        
        return path;
    }
    
    private static bool IsIdSegment(string segment)
    {
        // Check if segment is a GUID
        if (Guid.TryParse(segment, out _)) return true;
        
        // Check if segment is a number
        if (long.TryParse(segment, out _)) return true;
        
        // Check if segment is very long (likely an ID)
        if (segment.Length > 20) return true;
        
        return false;
    }
    
    private static Dictionary<string, string> ExtractRelevantHeaders(HttpContext context)
    {
        var headers = new Dictionary<string, string>();
        
        // Extract headers that might be relevant for rate limiting
        var relevantHeaders = new[]
        {
            "User-Agent",
            "Authorization",
            "X-API-Key",
            "Origin",
            "Referer"
        };
        
        foreach (var headerName in relevantHeaders)
        {
            var headerValue = context.Request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerValue))
            {
                headers[headerName] = headerValue;
            }
        }
        
        return headers;
    }
    
    private static void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        if (!result.IsAllowed || result.RequestLimit == 0) return;
        
        try
        {
            var headers = new RateLimitHeaders().ToHeaderDictionary(result);
            
            foreach (var header in headers)
            {
                context.Response.Headers[header.Key] = header.Value;
            }
        }
        catch (Exception)
        {
            // Ignore header addition errors
        }
    }
    
    private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitResult result)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        
        // Add retry-after header if available
        if (result.RetryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = result.RetryAfter.Value.TotalSeconds.ToString("0");
        }
        
        var response = new
        {
            error = "Rate limit exceeded",
            message = GetRateLimitMessage(result),
            type = result.ViolationType?.ToString() ?? "RateLimitExceeded",
            retryAfter = result.RetryAfter?.TotalSeconds,
            limit = result.RequestLimit,
            windowDuration = result.WindowDuration.TotalSeconds,
            identifier = result.Identifier
        };
        
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
        
        _logger.LogWarning("Rate limit exceeded: {Identifier} on {Endpoint} - {ViolationType}", 
            result.Identifier, result.Endpoint, result.ViolationType);
    }
    
    private static string GetRateLimitMessage(RateLimitResult result)
    {
        return result.ViolationType switch
        {
            RateLimitViolationType.CurrentlyBlocked => 
                $"Currently blocked due to previous violations. {result.BlockReason}",
            
            RateLimitViolationType.GlobalIPLimitExceeded => 
                $"Global IP rate limit exceeded. Limit: {result.RequestLimit} requests per day.",
            
            RateLimitViolationType.GlobalUserLimitExceeded => 
                $"Global user rate limit exceeded. Limit: {result.RequestLimit} requests per hour.",
            
            RateLimitViolationType.AnonymousLimitExceeded => 
                "Anonymous user rate limit exceeded. Please sign in for higher limits.",
            
            RateLimitViolationType.SuspiciousActivity => 
                "Suspicious activity detected. Access temporarily restricted.",
            
            _ => $"Rate limit exceeded. Limit: {result.RequestLimit} requests per {result.WindowDuration.TotalMinutes:0} minutes."
        };
    }
}