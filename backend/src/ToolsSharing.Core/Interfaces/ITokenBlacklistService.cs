using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface ITokenBlacklistService
{
    /// <summary>
    /// Blacklists a JWT token to prevent its further use
    /// </summary>
    Task<bool> BlacklistTokenAsync(string tokenId, string? userId, string reason, DateTime expiresAt, string? adminUserId = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null);
    
    /// <summary>
    /// Checks if a token is blacklisted
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string tokenId);
    
    /// <summary>
    /// Blacklists all tokens for a specific user
    /// </summary>
    Task<int> BlacklistAllUserTokensAsync(string userId, string reason, string? adminUserId = null);
    
    /// <summary>
    /// Blacklists all tokens issued before a specific date
    /// </summary>
    Task<int> BlacklistTokensIssuedBeforeAsync(DateTime cutoffDate, string reason, string? adminUserId = null);
    
    /// <summary>
    /// Gets blacklisted tokens for a user
    /// </summary>
    Task<List<BlacklistedToken>> GetUserBlacklistedTokensAsync(string userId);
    
    /// <summary>
    /// Gets all active blacklisted tokens
    /// </summary>
    Task<List<BlacklistedToken>> GetActiveBlacklistedTokensAsync();
    
    /// <summary>
    /// Removes expired blacklisted tokens from storage
    /// </summary>
    Task<int> CleanupExpiredTokensAsync();
    
    /// <summary>
    /// Gets blacklist statistics
    /// </summary>
    Task<TokenBlacklistStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Manually removes a token from blacklist (admin action)
    /// </summary>
    Task<bool> RemoveFromBlacklistAsync(string tokenId, string adminUserId, string reason);
    
    /// <summary>
    /// Gets blacklist details for a specific token
    /// </summary>
    Task<BlacklistedToken?> GetBlacklistDetailsAsync(string tokenId);
    
    /// <summary>
    /// Checks if token blacklisting is enabled
    /// </summary>
    bool IsBlacklistingEnabled { get; }
}

public class TokenBlacklistStatistics
{
    public int TotalBlacklistedTokens { get; set; }
    public int ActiveBlacklistedTokens { get; set; }
    public int ExpiredBlacklistedTokens { get; set; }
    public int TokensBlacklistedToday { get; set; }
    public Dictionary<string, int> BlacklistReasonCounts { get; set; } = new();
    public Dictionary<string, int> TokensByUser { get; set; } = new();
    public DateTime OldestBlacklistedToken { get; set; }
    public DateTime NewestBlacklistedToken { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}