using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.Entities;

public class SecurityEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = "";
    
    public string? UserId { get; set; }
    
    [MaxLength(256)]
    public string? UserEmail { get; set; }
    
    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = "";
    
    public string? UserAgent { get; set; }
    
    public bool Success { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    public string? GeographicLocation { get; set; } // JSON
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    [MaxLength(500)]
    public string? DeviceFingerprint { get; set; }
    
    public decimal? RiskScore { get; set; }
    
    [MaxLength(100)]
    public string? ResponseAction { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? AdditionalData { get; set; } // JSON for extra context
    
    // Navigation properties
    public virtual User? User { get; set; }
}

// Security event types
public static class SecurityEventTypes
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string PasswordChange = "PasswordChange";
    public const string PasswordReset = "PasswordReset";
    public const string AccountLockout = "AccountLockout";
    public const string AccountUnlock = "AccountUnlock";
    public const string SessionCreated = "SessionCreated";
    public const string SessionTerminated = "SessionTerminated";
    public const string TokenBlacklisted = "TokenBlacklisted";
    public const string SuspiciousActivity = "SuspiciousActivity";
    public const string BruteForceAttempt = "BruteForceAttempt";
    public const string GeographicAnomaly = "GeographicAnomaly";
    public const string SessionHijackingAttempt = "SessionHijackingAttempt";
    public const string ConcurrentSessionViolation = "ConcurrentSessionViolation";
    public const string VelocityAttack = "VelocityAttack";
    public const string SequentialAttack = "SequentialAttack";
    public const string DistributedAttack = "DistributedAttack";
    public const string DictionaryAttack = "DictionaryAttack";
}

// Response actions
public static class SecurityResponseActions
{
    public const string Allow = "Allow";
    public const string Block = "Block";
    public const string Challenge = "Challenge";
    public const string LockAccount = "LockAccount";
    public const string RequireCaptcha = "RequireCaptcha";
    public const string TerminateSession = "TerminateSession";
    public const string BlacklistToken = "BlacklistToken";
    public const string AlertAdmin = "AlertAdmin";
    public const string RequireReauth = "RequireReauth";
}