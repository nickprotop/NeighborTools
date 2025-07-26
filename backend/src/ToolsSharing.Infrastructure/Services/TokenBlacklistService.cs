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

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TokenBlacklistService> _logger;
    private readonly SessionSecurityOptions _options;

    public TokenBlacklistService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<TokenBlacklistService> logger,
        IOptions<SessionSecurityOptions> options)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsBlacklistingEnabled => _options.EnableSessionSecurity;

    public async Task<bool> BlacklistTokenAsync(string tokenId, string? userId, string reason, DateTime expiresAt, string? adminUserId = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null)
    {
        if (!IsBlacklistingEnabled)
        {
            _logger.LogWarning("Token blacklisting is disabled but BlacklistTokenAsync was called for token {TokenId}", tokenId);
            return false;
        }

        try
        {
            // Check if token is already blacklisted
            var existingEntry = await _context.BlacklistedTokens
                .FirstOrDefaultAsync(bt => bt.TokenId == tokenId && bt.IsActive);

            if (existingEntry != null)
            {
                _logger.LogInformation("Token {TokenId} is already blacklisted", tokenId);
                return true;
            }

            var blacklistedToken = new BlacklistedToken
            {
                TokenId = tokenId,
                UserId = userId,
                BlacklistedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Reason = reason,
                CreatedByUserId = adminUserId,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                IsActive = true,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    BlacklistedBy = adminUserId ?? "SYSTEM",
                    OriginalExpiration = expiresAt,
                    BlacklistTimestamp = DateTime.UtcNow
                })
            };

            _context.BlacklistedTokens.Add(blacklistedToken);
            await _context.SaveChangesAsync();

            // Cache the blacklisted status for quick lookup
            var cacheKey = $"token_blacklisted:{tokenId}";
            await _cache.SetStringAsync(cacheKey, "true", new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt.AddMinutes(5) // Cache slightly beyond token expiration
            });

            _logger.LogWarning("Token {TokenId} blacklisted for user {UserId}. Reason: {Reason}", tokenId, userId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token {TokenId}", tokenId);
            return false;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        if (!IsBlacklistingEnabled)
        {
            return false;
        }

        try
        {
            // Check cache first for performance
            var cacheKey = $"token_blacklisted:{tokenId}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            
            if (cachedResult != null)
            {
                return cachedResult == "true";
            }

            // Check database
            var isBlacklisted = await _context.BlacklistedTokens
                .AnyAsync(bt => bt.TokenId == tokenId && bt.IsActive && bt.ExpiresAt > DateTime.UtcNow);

            // Cache the result
            await _cache.SetStringAsync(cacheKey, isBlacklisted.ToString().ToLower(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return isBlacklisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token {TokenId} is blacklisted", tokenId);
            // Fail safe - if we can't check, assume it's not blacklisted to avoid blocking legitimate users
            return false;
        }
    }

    public async Task<int> BlacklistAllUserTokensAsync(string userId, string reason, string? adminUserId = null)
    {
        if (!IsBlacklistingEnabled)
        {
            return 0;
        }

        try
        {
            // Find all active sessions for the user
            var userSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var blacklistedCount = 0;

            foreach (var session in userSessions)
            {
                var success = await BlacklistTokenAsync(
                    session.SessionToken,
                    userId,
                    reason,
                    session.ExpiresAt,
                    adminUserId,
                    session.IPAddress,
                    session.UserAgent
                );

                if (success)
                {
                    blacklistedCount++;
                    
                    // Terminate the session
                    session.IsActive = false;
                    session.TerminatedAt = DateTime.UtcNow;
                    session.TerminationReason = SessionTerminationReasons.TokenBlacklisted;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Blacklisted {Count} tokens for user {UserId}. Reason: {Reason}", blacklistedCount, userId, reason);
            return blacklistedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting all tokens for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> BlacklistTokensIssuedBeforeAsync(DateTime cutoffDate, string reason, string? adminUserId = null)
    {
        if (!IsBlacklistingEnabled)
        {
            return 0;
        }

        try
        {
            // Find sessions created before the cutoff date
            var oldSessions = await _context.UserSessions
                .Where(s => s.CreatedAt < cutoffDate && s.IsActive)
                .ToListAsync();

            var blacklistedCount = 0;

            foreach (var session in oldSessions)
            {
                var success = await BlacklistTokenAsync(
                    session.SessionToken,
                    session.UserId,
                    reason,
                    session.ExpiresAt,
                    adminUserId
                );

                if (success)
                {
                    blacklistedCount++;
                    
                    // Terminate the session
                    session.IsActive = false;
                    session.TerminatedAt = DateTime.UtcNow;
                    session.TerminationReason = SessionTerminationReasons.TokenBlacklisted;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Blacklisted {Count} tokens issued before {CutoffDate}. Reason: {Reason}", 
                blacklistedCount, cutoffDate, reason);
            return blacklistedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting tokens issued before {CutoffDate}", cutoffDate);
            return 0;
        }
    }

    public async Task<List<BlacklistedToken>> GetUserBlacklistedTokensAsync(string userId)
    {
        try
        {
            return await _context.BlacklistedTokens
                .Where(bt => bt.UserId == userId)
                .OrderByDescending(bt => bt.BlacklistedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blacklisted tokens for user {UserId}", userId);
            return new List<BlacklistedToken>();
        }
    }

    public async Task<List<BlacklistedToken>> GetActiveBlacklistedTokensAsync()
    {
        try
        {
            return await _context.BlacklistedTokens
                .Where(bt => bt.IsActive && bt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(bt => bt.BlacklistedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active blacklisted tokens");
            return new List<BlacklistedToken>();
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.BlacklistedTokens
                .Where(bt => bt.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in expiredTokens)
            {
                token.IsActive = false;
                
                // Remove from cache if present
                var cacheKey = $"token_blacklisted:{token.TokenId}";
                await _cache.RemoveAsync(cacheKey);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired blacklisted tokens");
            return 0;
        }
    }

    public async Task<TokenBlacklistStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var tokens = await _context.BlacklistedTokens
                .Where(bt => bt.BlacklistedAt >= start && bt.BlacklistedAt <= end)
                .ToListAsync();

            var statistics = new TokenBlacklistStatistics
            {
                TotalBlacklistedTokens = tokens.Count,
                ActiveBlacklistedTokens = tokens.Count(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow),
                ExpiredBlacklistedTokens = tokens.Count(t => t.ExpiresAt <= DateTime.UtcNow),
                TokensBlacklistedToday = tokens.Count(t => t.BlacklistedAt.Date == DateTime.UtcNow.Date),
                PeriodStart = start,
                PeriodEnd = end
            };

            // Group by reason
            statistics.BlacklistReasonCounts = tokens
                .GroupBy(t => t.Reason)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by user (only include user ID for privacy)
            statistics.TokensByUser = tokens
                .Where(t => !string.IsNullOrEmpty(t.UserId))
                .GroupBy(t => t.UserId!)
                .ToDictionary(g => g.Key, g => g.Count());

            if (tokens.Any())
            {
                statistics.OldestBlacklistedToken = tokens.Min(t => t.BlacklistedAt);
                statistics.NewestBlacklistedToken = tokens.Max(t => t.BlacklistedAt);
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blacklist statistics");
            return new TokenBlacklistStatistics();
        }
    }

    public async Task<bool> RemoveFromBlacklistAsync(string tokenId, string adminUserId, string reason)
    {
        try
        {
            var blacklistedToken = await _context.BlacklistedTokens
                .FirstOrDefaultAsync(bt => bt.TokenId == tokenId && bt.IsActive);

            if (blacklistedToken == null)
            {
                _logger.LogWarning("Attempted to remove non-existent blacklisted token {TokenId}", tokenId);
                return false;
            }

            blacklistedToken.IsActive = false;
            
            // Update additional data to record the removal
            var additionalData = new
            {
                RemovedBy = adminUserId,
                RemovalReason = reason,
                RemovalTimestamp = DateTime.UtcNow,
                OriginalReason = blacklistedToken.Reason
            };
            
            blacklistedToken.AdditionalData = JsonSerializer.Serialize(additionalData);

            await _context.SaveChangesAsync();

            // Remove from cache
            var cacheKey = $"token_blacklisted:{tokenId}";
            await _cache.RemoveAsync(cacheKey);

            _logger.LogInformation("Token {TokenId} removed from blacklist by admin {AdminUserId}. Reason: {Reason}", 
                tokenId, adminUserId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing token {TokenId} from blacklist", tokenId);
            return false;
        }
    }

    public async Task<BlacklistedToken?> GetBlacklistDetailsAsync(string tokenId)
    {
        try
        {
            return await _context.BlacklistedTokens
                .Include(bt => bt.User)
                .Include(bt => bt.CreatedByUser)
                .FirstOrDefaultAsync(bt => bt.TokenId == tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blacklist details for token {TokenId}", tokenId);
            return null;
        }
    }
}