using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IBruteForceProtectionService
{
    /// <summary>
    /// Records a login attempt and analyzes for attack patterns
    /// </summary>
    Task<BruteForceAnalysisResult> RecordLoginAttemptAsync(string ipAddress, string? userEmail, bool success, string? userAgent = null, string? sessionId = null);
    
    /// <summary>
    /// Checks if an IP address is currently blocked due to brute force attempts
    /// </summary>
    Task<bool> IsIpBlockedAsync(string ipAddress);
    
    /// <summary>
    /// Checks if a user account is currently locked due to brute force attempts
    /// </summary>
    Task<bool> IsUserLockedAsync(string userEmail);
    
    /// <summary>
    /// Gets the remaining lockout duration for an IP address
    /// </summary>
    Task<TimeSpan?> GetIpLockoutRemainingAsync(string ipAddress);
    
    /// <summary>
    /// Gets the remaining lockout duration for a user account
    /// </summary>
    Task<TimeSpan?> GetUserLockoutRemainingAsync(string userEmail);
    
    /// <summary>
    /// Manually blocks an IP address
    /// </summary>
    Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason, string? adminUserId = null);
    
    /// <summary>
    /// Manually unlocks an IP address
    /// </summary>
    Task UnblockIpAddressAsync(string ipAddress, string? adminUserId = null);
    
    /// <summary>
    /// Manually locks a user account
    /// </summary>
    Task LockUserAccountAsync(string userEmail, TimeSpan duration, string reason, string? adminUserId = null);
    
    /// <summary>
    /// Manually unlocks a user account
    /// </summary>
    Task UnlockUserAccountAsync(string userEmail, string? adminUserId = null);
    
    /// <summary>
    /// Gets current attack patterns detected
    /// </summary>
    Task<List<AttackPattern>> GetActiveAttackPatternsAsync();
    
    /// <summary>
    /// Gets attack patterns for a specific source
    /// </summary>
    Task<List<AttackPattern>> GetAttackPatternsForSourceAsync(string sourceIdentifier);
    
    /// <summary>
    /// Clears expired attack patterns and lockouts
    /// </summary>
    Task CleanupExpiredDataAsync();
    
    /// <summary>
    /// Gets brute force protection statistics
    /// </summary>
    Task<BruteForceStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class BruteForceAnalysisResult
{
    public bool IsBlocked { get; set; }
    public bool RequiresCaptcha { get; set; }
    public string? BlockReason { get; set; }
    public TimeSpan? LockoutDuration { get; set; }
    public decimal RiskScore { get; set; }
    public List<string> DetectedPatterns { get; set; } = new();
    public bool ShouldAlertAdmin { get; set; }
    public string? RecommendedAction { get; set; }
}

public class BruteForceStatistics
{
    public int TotalLoginAttempts { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int BlockedIpAddresses { get; set; }
    public int LockedUserAccounts { get; set; }
    public int ActiveAttackPatterns { get; set; }
    public int SequentialAttacks { get; set; }
    public int DistributedAttacks { get; set; }
    public int VelocityAttacks { get; set; }
    public int DictionaryAttacks { get; set; }
    public Dictionary<string, int> AttacksByCountry { get; set; } = new();
    public Dictionary<string, int> TopTargetedAccounts { get; set; } = new();
    public Dictionary<string, int> TopAttackingSources { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}