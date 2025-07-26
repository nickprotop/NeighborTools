using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.Entities;

public class UserSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; } = "";
    
    [Required]
    [MaxLength(500)]
    public string SessionToken { get; set; } = "";
    
    [MaxLength(500)]
    public string? DeviceFingerprint { get; set; }
    
    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = "";
    
    public string? UserAgent { get; set; }
    
    public string? GeographicLocation { get; set; } // JSON
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(100)]
    public string? TerminationReason { get; set; }
    
    public DateTime? TerminatedAt { get; set; }
    
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    [MaxLength(50)]
    public string? Platform { get; set; }
    
    [MaxLength(100)]
    public string? Browser { get; set; }
    
    public bool IsSuspicious { get; set; } = false;
    
    public decimal? RiskScore { get; set; }
    
    public int ActivityCount { get; set; } = 0;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

// Session termination reasons
public static class SessionTerminationReasons
{
    public const string UserLogout = "UserLogout";
    public const string Expired = "Expired";
    public const string Inactivity = "Inactivity";
    public const string SuspiciousActivity = "SuspiciousActivity";
    public const string ConcurrentSessionLimit = "ConcurrentSessionLimit";
    public const string AdminTerminated = "AdminTerminated";
    public const string TokenBlacklisted = "TokenBlacklisted";
    public const string DeviceChanged = "DeviceChanged";
    public const string GeographicAnomaly = "GeographicAnomaly";
    public const string SessionHijacking = "SessionHijacking";
    public const string SecurityViolation = "SecurityViolation";
}