using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.Entities;

public class BlacklistedToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(500)]
    public string TokenId { get; set; } = "";
    
    public string? UserId { get; set; }
    
    [Required]
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = "";
    
    public string? CreatedByUserId { get; set; }
    
    [MaxLength(45)]
    public string? IPAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public string? AdditionalData { get; set; } // JSON
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual User? CreatedByUser { get; set; }
}

// Token blacklist reasons
public static class TokenBlacklistReasons
{
    public const string UserLogout = "UserLogout";
    public const string PasswordChanged = "PasswordChanged";
    public const string SuspiciousActivity = "SuspiciousActivity";
    public const string AdminAction = "AdminAction";
    public const string SecurityViolation = "SecurityViolation";
    public const string AccountLocked = "AccountLocked";
    public const string SessionHijacking = "SessionHijacking";
    public const string ConcurrentSessionViolation = "ConcurrentSessionViolation";
    public const string GeographicAnomaly = "GeographicAnomaly";
    public const string TokenCompromised = "TokenCompromised";
    public const string ManualRevocation = "ManualRevocation";
    public const string SystemMaintenance = "SystemMaintenance";
}