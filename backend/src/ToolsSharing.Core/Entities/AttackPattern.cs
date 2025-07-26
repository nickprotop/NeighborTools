using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.Entities;

public class AttackPattern
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string AttackType { get; set; } = "";
    
    [Required]
    [MaxLength(100)]
    public string SourceIdentifier { get; set; } = "";
    
    [MaxLength(100)]
    public string? TargetIdentifier { get; set; }
    
    public string? AttackData { get; set; } // JSON
    
    [Required]
    public DateTime FirstDetectedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime LastDetectedAt { get; set; } = DateTime.UtcNow;
    
    public int OccurrenceCount { get; set; } = 1;
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? ResolvedAt { get; set; }
    
    [MaxLength(200)]
    public string? ResolvedBy { get; set; }
    
    [MaxLength(500)]
    public string? ResolutionNotes { get; set; }
    
    public bool IsBlocked { get; set; } = false;
    
    public DateTime? BlockedAt { get; set; }
    
    public TimeSpan? BlockDuration { get; set; }
    
    public decimal? RiskScore { get; set; }
    
    public string? GeographicData { get; set; } // JSON
    
    public string? UserAgentPatterns { get; set; } // JSON
    
    public int SuccessfulAttempts { get; set; } = 0;
    
    public int FailedAttempts { get; set; } = 0;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Attack pattern types
public static class AttackPatternTypes
{
    public const string Sequential = "Sequential";
    public const string Distributed = "Distributed";
    public const string Velocity = "Velocity";
    public const string Dictionary = "Dictionary";
    public const string CredentialStuffing = "CredentialStuffing";
    public const string BotAttack = "BotAttack";
    public const string GeographicAnomaly = "GeographicAnomaly";
    public const string SessionHijacking = "SessionHijacking";
    public const string TokenReplay = "TokenReplay";
    public const string ConcurrentSessionAbuse = "ConcurrentSessionAbuse";
}

// Attack severity levels
public static class AttackSeverityLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}