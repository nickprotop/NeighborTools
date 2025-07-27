using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Configuration;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class BruteForceProtectionService : IBruteForceProtectionService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<BruteForceProtectionService> _logger;
    private readonly BruteForceProtectionOptions _options;
    private readonly IGeolocationService _geolocationService;

    public BruteForceProtectionService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<BruteForceProtectionService> logger,
        IOptions<BruteForceProtectionOptions> options,
        IGeolocationService geolocationService)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _geolocationService = geolocationService;
    }

    public async Task<BruteForceAnalysisResult> RecordLoginAttemptAsync(string ipAddress, string? userEmail, bool success, string? userAgent = null, string? sessionId = null)
    {
        if (!_options.EnableBruteForceProtection)
        {
            return new BruteForceAnalysisResult { IsBlocked = false, RiskScore = 0 };
        }

        try
        {
            var result = new BruteForceAnalysisResult();
            var now = DateTime.UtcNow;

            // Check if IP is already blocked
            var ipBlockedResult = await IsIpBlockedAsync(ipAddress);
            if (ipBlockedResult)
            {
                result.IsBlocked = true;
                result.BlockReason = "IP address is blocked due to previous brute force attempts";
                result.LockoutDuration = await GetIpLockoutRemainingAsync(ipAddress);
                return result;
            }

            // Check if user is already locked
            if (!string.IsNullOrEmpty(userEmail))
            {
                var userLockedResult = await IsUserLockedAsync(userEmail);
                if (userLockedResult)
                {
                    result.IsBlocked = true;
                    result.BlockReason = "User account is locked due to previous failed login attempts";
                    result.LockoutDuration = await GetUserLockoutRemainingAsync(userEmail);
                    return result;
                }
            }

            // Record the login attempt in security events
            await RecordSecurityEventAsync(ipAddress, userEmail, success, userAgent, sessionId);

            if (!success)
            {
                // Analyze failed attempt for attack patterns
                await AnalyzeFailedAttemptAsync(ipAddress, userEmail, userAgent, result);
            }
            else
            {
                // Reset failure counters on successful login
                await ResetFailureCountersAsync(ipAddress, userEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording login attempt for IP {IpAddress}", ipAddress);
            return new BruteForceAnalysisResult { IsBlocked = false, RiskScore = 0 };
        }
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        var cacheKey = $"brute_force_ip_blocked:{ipAddress}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            return bool.Parse(cachedResult);
        }

        // Check database for active attack patterns with blocking
        // Use client evaluation to handle DateTime.Add() since EF Core cannot translate it
        var candidateBlocks = await _context.AttackPatterns
            .Where(ap => ap.SourceIdentifier == ipAddress && 
                        ap.IsActive && 
                        ap.IsBlocked)
            .ToListAsync();
        
        var now = DateTime.UtcNow;
        var activeBlock = candidateBlocks.Any(ap => 
            !ap.BlockDuration.HasValue || 
            (ap.BlockedAt.HasValue && ap.BlockedAt.Value.Add(ap.BlockDuration.Value) > now));

        // Cache result for 1 minute
        await _cache.SetStringAsync(cacheKey, activeBlock.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });

        return activeBlock;
    }

    public async Task<bool> IsUserLockedAsync(string userEmail)
    {
        if (string.IsNullOrEmpty(userEmail)) return false;

        var cacheKey = $"brute_force_user_locked:{userEmail}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            return bool.Parse(cachedResult);
        }

        // Check database for active attack patterns targeting this user
        // Use client evaluation to handle DateTime.Add() since EF Core cannot translate it
        var candidateBlocks = await _context.AttackPatterns
            .Where(ap => ap.TargetIdentifier == userEmail && 
                        ap.IsActive && 
                        ap.IsBlocked)
            .ToListAsync();
        
        var now = DateTime.UtcNow;
        var activeBlock = candidateBlocks.Any(ap => 
            !ap.BlockDuration.HasValue || 
            (ap.BlockedAt.HasValue && ap.BlockedAt.Value.Add(ap.BlockDuration.Value) > now));

        // Cache result for 1 minute
        await _cache.SetStringAsync(cacheKey, activeBlock.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });

        return activeBlock;
    }

    public async Task<TimeSpan?> GetIpLockoutRemainingAsync(string ipAddress)
    {
        var attackPattern = await _context.AttackPatterns
            .Where(ap => ap.SourceIdentifier == ipAddress && 
                        ap.IsActive && 
                        ap.IsBlocked && 
                        ap.BlockedAt.HasValue && 
                        ap.BlockDuration.HasValue)
            .OrderByDescending(ap => ap.BlockedAt)
            .FirstOrDefaultAsync();

        if (attackPattern?.BlockedAt.HasValue == true && attackPattern.BlockDuration.HasValue)
        {
            var unblockTime = attackPattern.BlockedAt.Value.Add(attackPattern.BlockDuration.Value);
            var remaining = unblockTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }

        return null;
    }

    public async Task<TimeSpan?> GetUserLockoutRemainingAsync(string userEmail)
    {
        if (string.IsNullOrEmpty(userEmail)) return null;

        var attackPattern = await _context.AttackPatterns
            .Where(ap => ap.TargetIdentifier == userEmail && 
                        ap.IsActive && 
                        ap.IsBlocked && 
                        ap.BlockedAt.HasValue && 
                        ap.BlockDuration.HasValue)
            .OrderByDescending(ap => ap.BlockedAt)
            .FirstOrDefaultAsync();

        if (attackPattern?.BlockedAt.HasValue == true && attackPattern.BlockDuration.HasValue)
        {
            var unblockTime = attackPattern.BlockedAt.Value.Add(attackPattern.BlockDuration.Value);
            var remaining = unblockTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }

        return null;
    }

    public async Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason, string? adminUserId = null)
    {
        var attackPattern = new AttackPattern
        {
            AttackType = AttackPatternTypes.Sequential,
            SourceIdentifier = ipAddress,
            Severity = AttackSeverityLevels.High,
            IsBlocked = true,
            BlockedAt = DateTime.UtcNow,
            BlockDuration = duration,
            ResolutionNotes = reason,
            ResolvedBy = adminUserId
        };

        _context.AttackPatterns.Add(attackPattern);
        await _context.SaveChangesAsync();

        // Clear cache
        await _cache.RemoveAsync($"brute_force_ip_blocked:{ipAddress}");

        _logger.LogWarning("IP address {IpAddress} manually blocked for {Duration} by admin {AdminUserId}. Reason: {Reason}", 
            ipAddress, duration, adminUserId, reason);
    }

    public async Task UnblockIpAddressAsync(string ipAddress, string? adminUserId = null)
    {
        var activePatterns = await _context.AttackPatterns
            .Where(ap => ap.SourceIdentifier == ipAddress && ap.IsActive && ap.IsBlocked)
            .ToListAsync();

        foreach (var pattern in activePatterns)
        {
            pattern.IsBlocked = false;
            pattern.IsActive = false;
            pattern.ResolvedAt = DateTime.UtcNow;
            pattern.ResolvedBy = adminUserId;
            pattern.ResolutionNotes = "Manually unblocked by admin";
        }

        await _context.SaveChangesAsync();

        // Clear cache
        await _cache.RemoveAsync($"brute_force_ip_blocked:{ipAddress}");

        _logger.LogInformation("IP address {IpAddress} manually unblocked by admin {AdminUserId}", ipAddress, adminUserId);
    }

    public async Task LockUserAccountAsync(string userEmail, TimeSpan duration, string reason, string? adminUserId = null)
    {
        var attackPattern = new AttackPattern
        {
            AttackType = AttackPatternTypes.Sequential,
            TargetIdentifier = userEmail,
            SourceIdentifier = "ADMIN_ACTION",
            Severity = AttackSeverityLevels.High,
            IsBlocked = true,
            BlockedAt = DateTime.UtcNow,
            BlockDuration = duration,
            ResolutionNotes = reason,
            ResolvedBy = adminUserId
        };

        _context.AttackPatterns.Add(attackPattern);
        await _context.SaveChangesAsync();

        // Clear cache
        await _cache.RemoveAsync($"brute_force_user_locked:{userEmail}");

        _logger.LogWarning("User account {UserEmail} manually locked for {Duration} by admin {AdminUserId}. Reason: {Reason}", 
            userEmail, duration, adminUserId, reason);
    }

    public async Task UnlockUserAccountAsync(string userEmail, string? adminUserId = null)
    {
        var activePatterns = await _context.AttackPatterns
            .Where(ap => ap.TargetIdentifier == userEmail && ap.IsActive && ap.IsBlocked)
            .ToListAsync();

        foreach (var pattern in activePatterns)
        {
            pattern.IsBlocked = false;
            pattern.IsActive = false;
            pattern.ResolvedAt = DateTime.UtcNow;
            pattern.ResolvedBy = adminUserId;
            pattern.ResolutionNotes = "Manually unlocked by admin";
        }

        await _context.SaveChangesAsync();

        // Clear cache
        await _cache.RemoveAsync($"brute_force_user_locked:{userEmail}");

        _logger.LogInformation("User account {UserEmail} manually unlocked by admin {AdminUserId}", userEmail, adminUserId);
    }

    public async Task<List<AttackPattern>> GetActiveAttackPatternsAsync()
    {
        return await _context.AttackPatterns
            .Where(ap => ap.IsActive)
            .OrderByDescending(ap => ap.LastDetectedAt)
            .ToListAsync();
    }

    public async Task<List<AttackPattern>> GetAttackPatternsForSourceAsync(string sourceIdentifier)
    {
        return await _context.AttackPatterns
            .Where(ap => ap.SourceIdentifier == sourceIdentifier)
            .OrderByDescending(ap => ap.LastDetectedAt)
            .ToListAsync();
    }

    public async Task CleanupExpiredDataAsync()
    {
        var now = DateTime.UtcNow;
        var cutoffDate = now.AddDays(-_options.DataRetentionDays);
        int unblockedCount = 0;

        // First priority: Unblock expired patterns (this is what actually unblocks users/IPs)
        // Use client evaluation to handle DateTime.Add() since EF Core cannot translate it
        var candidatePatterns = await _context.AttackPatterns
            .Where(ap => ap.IsActive && 
                        ap.IsBlocked &&
                        ap.BlockedAt.HasValue && 
                        ap.BlockDuration.HasValue)
            .ToListAsync();
        
        // Filter expired patterns on client side
        var expiredPatterns = candidatePatterns
            .Where(ap => ap.BlockedAt!.Value.Add(ap.BlockDuration!.Value) < now)
            .ToList();

        foreach (var pattern in expiredPatterns)
        {
            // CRITICAL: Actually unblock the user/IP
            pattern.IsActive = false;
            pattern.IsBlocked = false;
            pattern.ResolvedAt = now;
            pattern.ResolutionNotes = "Automatically expired";
            
            // CRITICAL: Clear cache entries to ensure immediate unblocking
            if (!string.IsNullOrEmpty(pattern.SourceIdentifier) && pattern.SourceIdentifier != "ADMIN_ACTION")
            {
                await _cache.RemoveAsync($"brute_force_ip_blocked:{pattern.SourceIdentifier}");
            }
            if (!string.IsNullOrEmpty(pattern.TargetIdentifier))
            {
                await _cache.RemoveAsync($"brute_force_user_locked:{pattern.TargetIdentifier}");
            }
            
            unblockedCount++;
        }

        // Second priority: Clean up old security events (data retention)
        var oldEvents = await _context.SecurityEvents
            .Where(se => se.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.SecurityEvents.RemoveRange(oldEvents);

        await _context.SaveChangesAsync();

        if (unblockedCount > 0)
        {
            _logger.LogInformation("UNBLOCKED {UnblockedCount} expired patterns (users/IPs now accessible)", unblockedCount);
        }
        
        if (oldEvents.Count > 0)
        {
            _logger.LogInformation("Cleaned up {EventCount} old security events", oldEvents.Count);
        }
    }

    public async Task<BruteForceStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var events = await _context.SecurityEvents
            .Where(se => se.CreatedAt >= start && se.CreatedAt <= end)
            .ToListAsync();

        var attackPatterns = await _context.AttackPatterns
            .Where(ap => ap.FirstDetectedAt >= start && ap.FirstDetectedAt <= end)
            .ToListAsync();

        return new BruteForceStatistics
        {
            TotalLoginAttempts = events.Count(e => e.EventType == SecurityEventTypes.Login || e.EventType == SecurityEventTypes.LoginFailed),
            FailedLoginAttempts = events.Count(e => e.EventType == SecurityEventTypes.LoginFailed),
            BlockedIpAddresses = attackPatterns.Count(ap => ap.IsBlocked && !string.IsNullOrEmpty(ap.SourceIdentifier) && ap.SourceIdentifier != "ADMIN_ACTION"),
            LockedUserAccounts = attackPatterns.Count(ap => ap.IsBlocked && !string.IsNullOrEmpty(ap.TargetIdentifier)),
            ActiveAttackPatterns = attackPatterns.Count(ap => ap.IsActive),
            SequentialAttacks = attackPatterns.Count(ap => ap.AttackType == AttackPatternTypes.Sequential),
            DistributedAttacks = attackPatterns.Count(ap => ap.AttackType == AttackPatternTypes.Distributed),
            VelocityAttacks = attackPatterns.Count(ap => ap.AttackType == AttackPatternTypes.Velocity),
            DictionaryAttacks = attackPatterns.Count(ap => ap.AttackType == AttackPatternTypes.Dictionary),
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    public async Task<List<BlockedUserInfo>> GetBlockedUsersAsync()
    {
        var now = DateTime.UtcNow;
        var blockedUsers = await _context.AttackPatterns
            .Where(ap => ap.IsActive && ap.IsBlocked && !string.IsNullOrEmpty(ap.TargetIdentifier))
            .ToListAsync();

        return blockedUsers.Select(ap =>
        {
            var unblockAt = ap.BlockedAt?.Add(ap.BlockDuration ?? TimeSpan.Zero);
            var remaining = unblockAt.HasValue ? unblockAt.Value - now : (TimeSpan?)null;

            return new BlockedUserInfo
            {
                UserEmail = ap.TargetIdentifier!,
                BlockedAt = ap.BlockedAt ?? ap.FirstDetectedAt,
                UnblockAt = unblockAt,
                RemainingDuration = remaining > TimeSpan.Zero ? remaining : null,
                Reason = ap.ResolutionNotes ?? $"Brute force attack detected ({ap.AttackType})",
                AttackType = ap.AttackType,
                FailedAttempts = ap.FailedAttempts,
                BlockedBy = ap.ResolvedBy ?? "System"
            };
        }).ToList();
    }

    public async Task<List<BlockedIPInfo>> GetBlockedIPsAsync()
    {
        var now = DateTime.UtcNow;
        var blockedIPs = await _context.AttackPatterns
            .Where(ap => ap.IsActive && ap.IsBlocked && !string.IsNullOrEmpty(ap.SourceIdentifier) && ap.SourceIdentifier != "ADMIN_ACTION")
            .ToListAsync();

        return blockedIPs.Select(ap =>
        {
            var unblockAt = ap.BlockedAt?.Add(ap.BlockDuration ?? TimeSpan.Zero);
            var remaining = unblockAt.HasValue ? unblockAt.Value - now : (TimeSpan?)null;

            return new BlockedIPInfo
            {
                IPAddress = ap.SourceIdentifier!,
                BlockedAt = ap.BlockedAt ?? ap.FirstDetectedAt,
                UnblockAt = unblockAt,
                RemainingDuration = remaining > TimeSpan.Zero ? remaining : null,
                Reason = ap.ResolutionNotes ?? $"Brute force attack detected ({ap.AttackType})",
                AttackType = ap.AttackType,
                FailedAttempts = ap.FailedAttempts,
                BlockedBy = ap.ResolvedBy ?? "System",
                GeographicLocation = ap.GeographicData
            };
        }).ToList();
    }

    public async Task<SecurityCleanupStatus> GetCleanupStatusAsync()
    {
        var now = DateTime.UtcNow;
        
        // Get all active patterns to analyze
        var activePatterns = await _context.AttackPatterns
            .Where(ap => ap.IsActive)
            .ToListAsync();

        var blockedPatterns = activePatterns.Where(ap => ap.IsBlocked).ToList();
        var expiredPatterns = blockedPatterns.Where(ap =>
            ap.BlockedAt.HasValue &&
            ap.BlockDuration.HasValue &&
            ap.BlockedAt.Value.Add(ap.BlockDuration.Value) < now).ToList();

        var issues = new List<string>();
        
        // Check for expired patterns that should have been cleaned up
        if (expiredPatterns.Count > 0)
        {
            issues.Add($"{expiredPatterns.Count} expired patterns still marked as blocked");
        }

        // Check for very old patterns
        var oldPatterns = activePatterns.Where(ap => ap.FirstDetectedAt < now.AddDays(-7)).ToList();
        if (oldPatterns.Count > 10)
        {
            issues.Add($"{oldPatterns.Count} patterns older than 7 days still active");
        }

        return new SecurityCleanupStatus
        {
            ExpiredPatternsFound = expiredPatterns.Count,
            TotalActiveBlocks = blockedPatterns.Count,
            IsHealthy = issues.Count == 0,
            Issues = issues
        };
    }

    private async Task RecordSecurityEventAsync(string ipAddress, string? userEmail, bool success, string? userAgent, string? sessionId)
    {
        var geoLocation = await _geolocationService.GetLocationAsync(ipAddress);
        
        var securityEvent = new SecurityEvent
        {
            EventType = success ? SecurityEventTypes.Login : SecurityEventTypes.LoginFailed,
            UserEmail = userEmail,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            SessionId = sessionId,
            GeographicLocation = geoLocation != null ? JsonSerializer.Serialize(geoLocation) : null
        };

        _context.SecurityEvents.Add(securityEvent);
        await _context.SaveChangesAsync();
    }

    private async Task AnalyzeFailedAttemptAsync(string ipAddress, string? userEmail, string? userAgent, BruteForceAnalysisResult result)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-_options.DetectionWindowMinutes);

        // Get recent failed attempts from this IP
        var recentFailures = await _context.SecurityEvents
            .Where(se => se.IPAddress == ipAddress && 
                        se.EventType == SecurityEventTypes.LoginFailed && 
                        se.CreatedAt >= windowStart)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();

        var failureCount = recentFailures.Count;
        result.RiskScore = CalculateRiskScore(failureCount, recentFailures);

        // Check for sequential attack (same IP, multiple failures)
        if (failureCount >= _options.SequentialAttackThreshold)
        {
            await HandleSequentialAttackAsync(ipAddress, userEmail, failureCount, result);
        }

        // Check for distributed attack (multiple IPs targeting same user)
        if (!string.IsNullOrEmpty(userEmail))
        {
            var distributedFailures = await _context.SecurityEvents
                .Where(se => se.UserEmail == userEmail && 
                            se.EventType == SecurityEventTypes.LoginFailed && 
                            se.CreatedAt >= windowStart)
                .Select(se => se.IPAddress)
                .Distinct()
                .CountAsync();

            if (distributedFailures >= _options.DistributedAttackThreshold)
            {
                await HandleDistributedAttackAsync(userEmail, distributedFailures, result);
            }
        }

        // Check for velocity attack (too many attempts too quickly)
        var velocityWindow = now.AddMinutes(-_options.VelocityWindowMinutes);
        var velocityCount = recentFailures.Count(se => se.CreatedAt >= velocityWindow);
        
        if (velocityCount >= _options.VelocityAttackThreshold)
        {
            await HandleVelocityAttackAsync(ipAddress, userEmail, velocityCount, result);
        }
    }

    private decimal CalculateRiskScore(int failureCount, List<SecurityEvent> recentFailures)
    {
        decimal score = 0;

        // Base score from failure count
        score += Math.Min(failureCount * 10, 50);

        // Additional score for rapid succession
        if (recentFailures.Count >= 2)
        {
            var avgTimeBetween = recentFailures.Zip(recentFailures.Skip(1), (a, b) => (a.CreatedAt - b.CreatedAt).TotalSeconds).Average();
            if (avgTimeBetween < 30) score += 20; // Very rapid attempts
            else if (avgTimeBetween < 60) score += 10; // Rapid attempts
        }

        // Score for geographic anomalies (simplified)
        var uniqueCountries = recentFailures
            .Where(se => !string.IsNullOrEmpty(se.GeographicLocation))
            .Select(se => se.GeographicLocation)
            .Distinct()
            .Count();
        
        if (uniqueCountries > 1) score += 15;

        return Math.Min(score, 100);
    }

    private async Task HandleSequentialAttackAsync(string ipAddress, string? userEmail, int failureCount, BruteForceAnalysisResult result)
    {
        result.DetectedPatterns.Add(AttackPatternTypes.Sequential);

        var existingPattern = await _context.AttackPatterns
            .FirstOrDefaultAsync(ap => ap.SourceIdentifier == ipAddress && 
                                     ap.AttackType == AttackPatternTypes.Sequential && 
                                     ap.IsActive);

        if (existingPattern != null)
        {
            existingPattern.OccurrenceCount++;
            existingPattern.LastDetectedAt = DateTime.UtcNow;
            existingPattern.FailedAttempts = failureCount;
        }
        else
        {
            var pattern = new AttackPattern
            {
                AttackType = AttackPatternTypes.Sequential,
                SourceIdentifier = ipAddress,
                TargetIdentifier = userEmail,
                Severity = failureCount >= _options.MaxFailedAttemptsBeforeLockout ? AttackSeverityLevels.High : AttackSeverityLevels.Medium,
                FailedAttempts = failureCount,
                RiskScore = result.RiskScore
            };

            _context.AttackPatterns.Add(pattern);
            existingPattern = pattern;
        }

        // Check if we should block
        if (failureCount >= _options.MaxFailedAttemptsBeforeLockout)
        {
            existingPattern.IsBlocked = true;
            existingPattern.BlockedAt = DateTime.UtcNow;
            existingPattern.BlockDuration = _options.AccountLockoutDuration;

            result.IsBlocked = true;
            result.BlockReason = "Too many failed login attempts";
            result.LockoutDuration = _options.AccountLockoutDuration;
            result.ShouldAlertAdmin = true;
        }
        else if (failureCount >= _options.CaptchaThreshold)
        {
            result.RequiresCaptcha = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandleDistributedAttackAsync(string userEmail, int sourceCount, BruteForceAnalysisResult result)
    {
        result.DetectedPatterns.Add(AttackPatternTypes.Distributed);

        var existingPattern = await _context.AttackPatterns
            .FirstOrDefaultAsync(ap => ap.TargetIdentifier == userEmail && 
                                     ap.AttackType == AttackPatternTypes.Distributed && 
                                     ap.IsActive);

        if (existingPattern != null)
        {
            existingPattern.OccurrenceCount++;
            existingPattern.LastDetectedAt = DateTime.UtcNow;
        }
        else
        {
            var pattern = new AttackPattern
            {
                AttackType = AttackPatternTypes.Distributed,
                SourceIdentifier = "MULTIPLE_IPS",
                TargetIdentifier = userEmail,
                Severity = AttackSeverityLevels.High,
                RiskScore = result.RiskScore,
                AttackData = JsonSerializer.Serialize(new { SourceCount = sourceCount })
            };

            _context.AttackPatterns.Add(pattern);
            existingPattern = pattern;
        }

        // Lock the targeted user account
        existingPattern.IsBlocked = true;
        existingPattern.BlockedAt = DateTime.UtcNow;
        existingPattern.BlockDuration = _options.AccountLockoutDuration;

        result.IsBlocked = true;
        result.BlockReason = "Distributed attack detected on user account";
        result.LockoutDuration = _options.AccountLockoutDuration;
        result.ShouldAlertAdmin = true;

        await _context.SaveChangesAsync();
    }

    private async Task HandleVelocityAttackAsync(string ipAddress, string? userEmail, int attemptCount, BruteForceAnalysisResult result)
    {
        result.DetectedPatterns.Add(AttackPatternTypes.Velocity);

        var pattern = new AttackPattern
        {
            AttackType = AttackPatternTypes.Velocity,
            SourceIdentifier = ipAddress,
            TargetIdentifier = userEmail,
            Severity = AttackSeverityLevels.Medium,
            RiskScore = result.RiskScore,
            AttackData = JsonSerializer.Serialize(new { AttemptCount = attemptCount, WindowMinutes = _options.VelocityWindowMinutes })
        };

        _context.AttackPatterns.Add(pattern);

        // Require CAPTCHA for velocity attacks
        result.RequiresCaptcha = true;
        result.RecommendedAction = "Require CAPTCHA verification";

        await _context.SaveChangesAsync();
    }

    private async Task ResetFailureCountersAsync(string ipAddress, string? userEmail)
    {
        // Deactivate recent attack patterns for successful login
        var recentPatterns = await _context.AttackPatterns
            .Where(ap => (ap.SourceIdentifier == ipAddress || ap.TargetIdentifier == userEmail) && 
                        ap.IsActive && 
                        !ap.IsBlocked &&
                        ap.LastDetectedAt >= DateTime.UtcNow.AddHours(-1))
            .ToListAsync();

        foreach (var pattern in recentPatterns)
        {
            pattern.IsActive = false;
            pattern.ResolvedAt = DateTime.UtcNow;
            pattern.ResolutionNotes = "Successful login - threat resolved";
        }

        if (recentPatterns.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}