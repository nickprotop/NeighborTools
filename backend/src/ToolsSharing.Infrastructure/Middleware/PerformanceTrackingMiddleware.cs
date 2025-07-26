using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.Infrastructure.Middleware;

public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public PerformanceTrackingMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Record the response time
            using var scope = _serviceProvider.CreateScope();
            var performanceService = scope.ServiceProvider.GetService<IPerformanceMetricsService>();
            
            if (performanceService != null)
            {
                performanceService.RecordResponseTime(stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}