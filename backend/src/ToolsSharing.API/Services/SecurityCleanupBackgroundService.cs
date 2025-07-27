using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Services;

namespace ToolsSharing.API.Services;

/// <summary>
/// Background service that ensures blocked users and IPs are automatically unblocked when their lockout expires
/// </summary>
public class SecurityCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityCleanupBackgroundService> _logger;
    
    // Fast cleanup for active blocking - runs every 1 minute to ensure rapid unblocking
    private readonly TimeSpan _activeCleanupInterval = TimeSpan.FromMinutes(1);
    
    // Bulk cleanup for data retention - runs every 30 minutes for efficiency
    private readonly TimeSpan _bulkCleanupInterval = TimeSpan.FromMinutes(30);
    
    private DateTime _lastBulkCleanup = DateTime.MinValue;
    private DateTime _lastActiveCleanup = DateTime.MinValue;

    public SecurityCleanupBackgroundService(
        IServiceProvider serviceProvider, 
        ILogger<SecurityCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SecurityCleanupBackgroundService started - ensuring blocked users/IPs get unblocked automatically");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Always run active cleanup (expired blocks) - CRITICAL for unblocking
                await RunActiveCleanup();
                _lastActiveCleanup = now;
                
                // Run bulk cleanup every 30 minutes
                if (now - _lastBulkCleanup >= _bulkCleanupInterval)
                {
                    await RunBulkCleanup();
                    _lastBulkCleanup = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR in security cleanup cycle - this may prevent automatic unblocking!");
            }

            await Task.Delay(_activeCleanupInterval, stoppingToken);
        }
    }

    /// <summary>
    /// CRITICAL METHOD: This actually unblocks expired users and IPs
    /// </summary>
    private async Task RunActiveCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var bruteForceService = scope.ServiceProvider.GetRequiredService<IBruteForceProtectionService>();
        
        try
        {
            // This method MUST unblock expired patterns and clear cache
            await bruteForceService.CleanupExpiredDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Active cleanup failed - blocked users/IPs may remain blocked longer than intended!");
        }
    }

    /// <summary>
    /// Bulk cleanup for data retention and performance optimization
    /// </summary>
    private async Task RunBulkCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();
            var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            var blacklistService = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
            
            // Clean up rate limit data
            await rateLimitService.CleanupExpiredDataAsync();
            
            // Clean up expired tokens
            var expiredTokens = await refreshTokenService.CleanupExpiredTokensAsync();
            var expiredBlacklist = await blacklistService.CleanupExpiredTokensAsync();
            
            _logger.LogInformation("Bulk cleanup completed: {ExpiredTokens} tokens, {ExpiredBlacklist} blacklist entries", 
                expiredTokens, expiredBlacklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk cleanup failed - performance may be impacted but security blocking unaffected");
        }
    }

    /// <summary>
    /// Get status for admin monitoring
    /// </summary>
    public SecurityCleanupServiceStatus GetStatus()
    {
        return new SecurityCleanupServiceStatus
        {
            IsRunning = true,
            LastActiveCleanup = _lastActiveCleanup,
            LastBulkCleanup = _lastBulkCleanup,
            NextActiveCleanup = _lastActiveCleanup.Add(_activeCleanupInterval),
            NextBulkCleanup = _lastBulkCleanup.Add(_bulkCleanupInterval),
            ActiveCleanupInterval = _activeCleanupInterval,
            BulkCleanupInterval = _bulkCleanupInterval
        };
    }
}

public class SecurityCleanupServiceStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastActiveCleanup { get; set; }
    public DateTime LastBulkCleanup { get; set; }
    public DateTime NextActiveCleanup { get; set; }
    public DateTime NextBulkCleanup { get; set; }
    public TimeSpan ActiveCleanupInterval { get; set; }
    public TimeSpan BulkCleanupInterval { get; set; }
}