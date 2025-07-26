using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Services;
using ToolsSharing.Infrastructure.Middleware;

namespace ToolsSharing.Infrastructure.Extensions;

public static class SecurityExtensions
{
    /// <summary>
    /// Adds Phase 1 security services to the DI container
    /// </summary>
    public static IServiceCollection AddPhase1Security(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure security options
        services.Configure<RequestSecurityOptions>(
            configuration.GetSection(RequestSecurityOptions.SectionName));
        services.Configure<IPSecurityOptions>(
            configuration.GetSection(IPSecurityOptions.SectionName));
        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection(SecurityHeadersOptions.SectionName));

        // Register security services as singletons for middleware injection
        services.AddSingleton<IIPSecurityService, IPSecurityService>();
        services.AddSingleton<IGeolocationService, GeolocationService>();
        
        // Add HTTP client for geolocation service (ipwho.is)
        services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "NeighborTools-Security/1.0");
            client.BaseAddress = new Uri("https://ipwho.is/");
        });

        // Add distributed cache for IP blocking (Redis)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "NeighborTools-Security";
            });
        }
        else
        {
            // Fallback to in-memory cache if Redis is not configured
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Adds Phase 1 security middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UsePhase1Security(this IApplicationBuilder app)
    {
        // Apply security middleware in the correct order
        // ORDER IS CRITICAL - each middleware should reject requests as early as possible
        
        // 1. Request size limits (fastest rejection)
        app.UseMiddleware<RequestSizeLimitMiddleware>();
        
        // 2. IP blocking and geographic filtering  
        app.UseMiddleware<IPSecurityMiddleware>();
        
        // 3. Security headers (applied to all responses)
        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }

    /// <summary>
    /// Validates security configuration on startup
    /// </summary>
    public static void ValidateSecurityConfiguration(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("SecurityValidation");
        
        try
        {
            // Validate Redis connection if configured
            var cache = scope.ServiceProvider.GetService<IDistributedCache>();
            if (cache != null)
            {
                // Test cache connectivity
                cache.SetStringAsync("security_test", "ok", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
                }).GetAwaiter().GetResult();
                logger?.LogInformation("✅ Security cache (Redis) connection validated");
            }

            // Validate geolocation service
            var geoService = scope.ServiceProvider.GetService<IGeolocationService>();
            if (geoService != null)
            {
                logger?.LogInformation("✅ Geolocation service registered");
            }

            // Validate IP security service
            var ipService = scope.ServiceProvider.GetService<IIPSecurityService>();
            if (ipService != null)
            {
                logger?.LogInformation("✅ IP security service registered");
            }

            logger?.LogInformation("🔒 Phase 1 Security validation completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Security validation failed");
            throw;
        }
    }
}