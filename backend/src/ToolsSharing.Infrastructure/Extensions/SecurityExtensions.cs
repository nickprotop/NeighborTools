using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using ToolsSharing.Core.Interfaces;
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
        // gitleaks:allow
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
    /// Adds Phase 2 security services to the DI container
    /// </summary>
    public static IServiceCollection AddPhase2Security(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Phase 2 security options
        services.Configure<RateLimitOptions>(
            configuration.GetSection(RateLimitOptions.SectionName));
        
        // Register Phase 2 security services
        services.AddSingleton<IRateLimitService, RateLimitService>();
        
        return services;
    }

    /// <summary>
    /// Adds Phase 3 security services to the DI container
    /// </summary>
    public static IServiceCollection AddPhase3Security(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Phase 3 security options
        services.Configure<BruteForceProtectionOptions>(
            configuration.GetSection(BruteForceProtectionOptions.SectionName));
        services.Configure<SessionSecurityOptions>(
            configuration.GetSection(SessionSecurityOptions.SectionName));
        services.Configure<AuthenticationLoggingOptions>(
            configuration.GetSection(AuthenticationLoggingOptions.SectionName));
        
        // Register Phase 3 Core Security Services
        services.AddScoped<IBruteForceProtectionService, BruteForceProtectionService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<IAuthenticationEventLogger, AuthenticationEventLogger>();
        
        // Register Phase 3 Security Analytics Services
        services.AddScoped<ISecurityAnalyticsService, SecurityAnalyticsService>();
        services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();
        
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
    /// Adds Phase 2 security middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UsePhase2Security(this IApplicationBuilder app)
    {
        // Apply Phase 2 security middleware after Phase 1
        // ORDER IS CRITICAL - Phase 2 builds on Phase 1 protection
        
        // 1. Rate limiting (applied after IP blocking)
        app.UseMiddleware<RateLimitMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Adds Phase 3 security middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UsePhase3Security(this IApplicationBuilder app)
    {
        // Apply Phase 3 security middleware after Phase 2
        // ORDER IS CRITICAL - Phase 3 builds on Phase 1 & 2 protection
        
        // 1. Performance tracking (measures all requests)
        app.UseMiddleware<PerformanceTrackingMiddleware>();
        
        // 2. Session security validation (applied after rate limiting, before authentication)
        app.UseMiddleware<SessionSecurityMiddleware>();
        
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

    /// <summary>
    /// Validates Phase 2 security configuration on startup
    /// </summary>
    public static void ValidatePhase2SecurityConfiguration(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("Phase2SecurityValidation");
        
        try
        {
            // Validate rate limiting service
            var rateLimitService = scope.ServiceProvider.GetService<IRateLimitService>();
            if (rateLimitService != null)
            {
                logger?.LogInformation("✅ Rate limiting service registered");
            }

            logger?.LogInformation("🔒 Phase 2 Security validation completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Phase 2 Security validation failed");
            throw;
        }
    }

    /// <summary>
    /// Validates Phase 3 security configuration on startup
    /// </summary>
    public static void ValidatePhase3SecurityConfiguration(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("Phase3SecurityValidation");
        
        try
        {
            // Validate brute force protection service
            var bruteForceService = scope.ServiceProvider.GetService<IBruteForceProtectionService>();
            if (bruteForceService != null)
            {
                logger?.LogInformation("✅ Brute force protection service registered");
            }

            // Validate token blacklist service
            var tokenBlacklistService = scope.ServiceProvider.GetService<ITokenBlacklistService>();
            if (tokenBlacklistService != null)
            {
                logger?.LogInformation("✅ Token blacklist service registered");
            }

            // Validate authentication event logger
            var authEventLogger = scope.ServiceProvider.GetService<IAuthenticationEventLogger>();
            if (authEventLogger != null)
            {
                logger?.LogInformation("✅ Authentication event logger registered");
            }

            logger?.LogInformation("🔒 Phase 3 Security validation completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ Phase 3 Security validation failed");
            throw;
        }
    }
}