using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;

namespace ToolsSharing.Infrastructure.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Apply security headers before processing request
        ApplySecurityHeaders(context);

        await _next(context);
    }

    private void ApplySecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // X-Content-Type-Options: Prevent MIME type sniffing
        if (_options.EnableContentTypeOptions)
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options: Prevent clickjacking
        if (_options.EnableFrameOptions)
        {
            headers["X-Frame-Options"] = "DENY";
        }

        // X-XSS-Protection: Enable XSS filtering (legacy but still useful)
        if (_options.EnableXSSProtection)
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Referrer-Policy: Control referrer information
        if (!string.IsNullOrEmpty(_options.ReferrerPolicy))
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
        }

        // X-Permitted-Cross-Domain-Policies: Disable Adobe Flash and PDF handlers
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Content-Security-Policy: Control resource loading (very restrictive for API)
        if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // Strict-Transport-Security: Enforce HTTPS (only for HTTPS connections)
        if (context.Request.IsHttps && _options.EnableHSTS)
        {
            var hstsValue = $"max-age={_options.HSTSMaxAge}";
            if (_options.HSTSIncludeSubDomains)
            {
                hstsValue += "; includeSubDomains";
            }
            if (_options.HSTSPreload)
            {
                hstsValue += "; preload";
            }
            headers["Strict-Transport-Security"] = hstsValue;
        }

        // Permissions-Policy: Control browser features
        if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Cache control for API responses (prevent caching of sensitive data)
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        // Add custom headers from configuration
        foreach (var customHeader in _options.CustomHeaders)
        {
            headers[customHeader.Key] = customHeader.Value;
        }

        // X-Request-ID: Add unique request identifier for tracking
        if (!headers.ContainsKey("X-Request-ID"))
        {
            headers["X-Request-ID"] = Guid.NewGuid().ToString();
        }

        // X-Response-Time: Will be set by timing middleware if available
        // Security headers applied timestamp
        headers["X-Security-Headers-Applied"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}