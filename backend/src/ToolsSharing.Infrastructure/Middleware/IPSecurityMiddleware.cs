using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Infrastructure.Middleware;

public class IPSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIPSecurityService _ipSecurity;
    private readonly IPSecurityOptions _options;
    private readonly ILogger<IPSecurityMiddleware> _logger;

    public IPSecurityMiddleware(
        RequestDelegate next,
        IIPSecurityService ipSecurity,
        IOptions<IPSecurityOptions> options,
        ILogger<IPSecurityMiddleware> logger)
    {
        _next = next;
        _ipSecurity = ipSecurity;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnableIPBlocking && !_options.EnableGeolocation)
        {
            await _next(context);
            return;
        }

        var clientIP = GetClientIP(context);
        
        // Skip security checks for trusted proxies and local IPs
        if (IsTrustedIP(clientIP))
        {
            await _next(context);
            return;
        }

        try
        {
            // Fast IP blocking check (Redis cache lookup)
            var blockResult = await _ipSecurity.IsIPBlockedAsync(clientIP);
            if (blockResult.IsBlocked)
            {
                _logger.LogWarning("Blocked IP {ClientIP} attempted access to {Path}: {Reason} (Expires: {ExpiresAt})", 
                    clientIP, context.Request.Path, blockResult.Reason, blockResult.ExpiresAt);
                
                context.Response.StatusCode = 403; // Forbidden
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    Success = false,
                    Message = "Access denied",
                    Error = new
                    {
                        Code = "IP_BLOCKED",
                        Reason = "Your IP address has been blocked due to suspicious activity",
                        ExpiresAt = blockResult.IsPermanent ? null : blockResult.ExpiresAt
                    }
                };
                
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                return;
            }

            // Geographic filtering for sensitive endpoints
            if (_options.EnableGeolocation && IsSensitiveEndpoint(context.Request.Path))
            {
                var geoResult = await _ipSecurity.CheckGeolocationAsync(clientIP);
                if (!geoResult.IsAllowed)
                {
                    _logger.LogWarning("Geographic access denied for IP {ClientIP} from {Country} accessing {Path}", 
                        clientIP, geoResult.CountryCode, context.Request.Path);
                    
                    // Track this as a minor offense (geographic restriction violation)
                    await _ipSecurity.IncrementOffenseCountAsync(clientIP, $"Geographic restriction violation from {geoResult.CountryCode}");
                    
                    context.Response.StatusCode = 451; // Unavailable For Legal Reasons
                    context.Response.ContentType = "application/json";
                    
                    var geoErrorResponse = new
                    {
                        Success = false,
                        Message = "Service not available in your region",
                        Error = new
                        {
                            Code = "GEOGRAPHIC_RESTRICTION",
                            Country = geoResult.CountryCode,
                            Reason = "This service is not available in your geographic location"
                        }
                    };
                    
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(geoErrorResponse));
                    return;
                }

                // Add geographic info to context for downstream middleware and logging
                context.Items["ClientCountry"] = geoResult.CountryCode;
                context.Items["ClientCity"] = geoResult.City;
                context.Items["IsFromVPN"] = geoResult.IsFromVPN;
                context.Items["IsFromProxy"] = geoResult.IsFromProxy;
            }

            // Add IP info to context for downstream middleware
            context.Items["ClientIP"] = clientIP;
            context.Items["IPSecurityChecked"] = true;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IP security middleware for IP {ClientIP}", clientIP);
            
            // On error, allow request to continue but log the issue
            // This prevents security middleware from breaking the application
            await _next(context);
        }
    }

    private string GetClientIP(HttpContext context)
    {
        // Check X-Forwarded-For header (for requests through load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            var firstIP = forwardedFor.Split(',')[0].Trim();
            if (IsValidIP(firstIP))
            {
                return firstIP;
            }
        }

        // Check X-Real-IP header (used by some proxies)
        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP) && IsValidIP(realIP))
        {
            return realIP;
        }

        // Check CF-Connecting-IP header (Cloudflare)
        var cfIP = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIP) && IsValidIP(cfIP))
        {
            return cfIP;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsTrustedIP(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
            return false;

        return _options.TrustedProxies.Contains(ipAddress);
    }

    private static bool IsValidIP(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/auth/",
            "/api/payments/",
            "/api/admin/",
            "/api/disputes/",
            "/api/user/",
            "/api/settings/"
        };

        return sensitiveEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}