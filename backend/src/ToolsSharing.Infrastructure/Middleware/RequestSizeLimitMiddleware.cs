using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Models;

namespace ToolsSharing.Infrastructure.Middleware;

public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestSecurityOptions _options;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        IOptions<RequestSecurityOptions> options,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var validationResult = ValidateRequestSize(context);
        
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Request rejected: {Reason}. Endpoint: {Endpoint}, Actual Size: {ActualSize}, Max Allowed: {MaxAllowed}, IP: {ClientIP}",
                validationResult.Reason,
                validationResult.Endpoint,
                validationResult.ActualSize,
                validationResult.MaxAllowedSize,
                GetClientIP(context));
            
            context.Response.StatusCode = 413; // Payload Too Large
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                Success = false,
                Message = "Request payload too large",
                Error = new
                {
                    Code = "PAYLOAD_TOO_LARGE",
                    MaxSize = validationResult.MaxAllowedSize,
                    ActualSize = validationResult.ActualSize
                }
            };
            
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            return;
        }

        // Set request body size limit for this specific endpoint
        var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature != null)
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = validationResult.MaxAllowedSize;
        }

        // Set request timeout
        using var cts = new CancellationTokenSource(_options.RequestTimeout);
        var originalToken = context.RequestAborted;
        
        try
        {
            context.RequestAborted = CancellationTokenSource
                .CreateLinkedTokenSource(originalToken, cts.Token).Token;

            await _next(context);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Request timeout for {Path} from IP {ClientIP}", 
                context.Request.Path, GetClientIP(context));
            
            context.Response.StatusCode = 408; // Request Timeout
            await context.Response.WriteAsync("Request timeout");
        }
    }

    private RequestSizeValidationResult ValidateRequestSize(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var maxSize = GetMaxSizeForEndpoint(path);
        var contentLength = context.Request.ContentLength ?? 0;

        var result = new RequestSizeValidationResult
        {
            ActualSize = contentLength,
            MaxAllowedSize = maxSize,
            Endpoint = path,
            IsValid = true
        };

        // Check Content-Length header first (fastest check)
        if (contentLength > maxSize)
        {
            result.IsValid = false;
            result.Reason = $"Content-Length {contentLength} exceeds limit {maxSize} for endpoint {path}";
        }

        return result;
    }

    private long GetMaxSizeForEndpoint(string path)
    {
        // Check for specific endpoint limits (most specific first)
        foreach (var limit in _options.EndpointLimits.OrderByDescending(x => x.Key.Length))
        {
            if (path.StartsWith(limit.Key, StringComparison.OrdinalIgnoreCase))
            {
                return limit.Value;
            }
        }
        
        return _options.DefaultMaxRequestBodySize;
    }

    private static string GetClientIP(HttpContext context)
    {
        // Check for forwarded IPs (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            return realIP;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}