namespace frontend.Models;

public class BlockedUserInfo
{
    public string UserEmail { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime? UnblockAt { get; set; }
    public TimeSpan? RemainingDuration { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public string? BlockedBy { get; set; }
}

public class BlockedIPInfo
{
    public string IPAddress { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime? UnblockAt { get; set; }
    public TimeSpan? RemainingDuration { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public string? BlockedBy { get; set; }
    public string? GeographicLocation { get; set; }
}

public class SecurityCleanupStatus
{
    public DateTime? LastCleanupRun { get; set; }
    public DateTime? LastActiveCleanupRun { get; set; }
    public DateTime? LastBulkCleanupRun { get; set; }
    public int ExpiredPatternsFound { get; set; }
    public int PatternsUnblocked { get; set; }
    public int TotalActiveBlocks { get; set; }
    public bool IsHealthy { get; set; }
    public List<string> Issues { get; set; } = new();
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

public class SecurityCleanupStatusResponse
{
    public SecurityCleanupServiceStatus ServiceStatus { get; set; } = new();
    public SecurityCleanupStatus SecurityStatus { get; set; } = new();
    public bool OverallHealth { get; set; }
    public DateTime LastCheck { get; set; }
}

public class CleanupResult
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int ExpiredPatternsFoundBefore { get; set; }
    public int ExpiredPatternsFoundAfter { get; set; }
    public int PatternsUnblocked { get; set; }
    public int TotalActiveBlocksBefore { get; set; }
    public int TotalActiveBlocksAfter { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public TimeSpan Duration => CompletedAt - StartedAt;
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