namespace ToolsSharing.Infrastructure.Configuration;

public class BruteForceProtectionOptions
{
    public const string SectionName = "BruteForceProtection";
    
    /// <summary>
    /// Enable/disable brute force protection globally
    /// </summary>
    public bool EnableBruteForceProtection { get; set; } = true;
    
    /// <summary>
    /// Maximum failed login attempts before account lockout
    /// </summary>
    public int MaxFailedAttemptsBeforeLockout { get; set; } = 5;
    
    /// <summary>
    /// Initial account lockout duration
    /// </summary>
    public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Multiplier for progressive lockout durations
    /// </summary>
    public double ProgressiveLockoutMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Maximum lockout duration
    /// </summary>
    public TimeSpan MaxLockoutDuration { get; set; } = TimeSpan.FromHours(24);
    
    /// <summary>
    /// Number of failed attempts before requiring CAPTCHA
    /// </summary>
    public int EnableCaptchaAfterAttempts { get; set; } = 3;
    
    /// <summary>
    /// Sequential attack threshold (same IP, multiple accounts)
    /// </summary>
    public int SequentialAttackThreshold { get; set; } = 5;
    
    /// <summary>
    /// Distributed attack threshold (multiple IPs, same account)
    /// </summary>
    public int DistributedAttackThreshold { get; set; } = 10;
    
    /// <summary>
    /// Time window for analyzing suspicious activity
    /// </summary>
    public TimeSpan SuspiciousActivityWindow { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Velocity attack threshold (rapid attempts)
    /// </summary>
    public int VelocityAttackThreshold { get; set; } = 3;
    
    /// <summary>
    /// Time window for velocity attack detection
    /// </summary>
    public TimeSpan VelocityAttackWindow { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Enable dictionary/common password attack detection
    /// </summary>
    public bool DictionaryAttackDetection { get; set; } = true;
    
    /// <summary>
    /// Threshold for common password attempts
    /// </summary>
    public int CommonPasswordThreshold { get; set; } = 10;
    
    /// <summary>
    /// Trusted IP addresses that bypass brute force protection
    /// </summary>
    public List<string> WhitelistTrustedIPs { get; set; } = new();
    
    /// <summary>
    /// Alert thresholds for monitoring
    /// </summary>
    public BruteForceAlertThresholds AlertThresholds { get; set; } = new();
    
    /// <summary>
    /// Time window for tracking failed attempts
    /// </summary>
    public TimeSpan FailedAttemptWindow { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Reset lockout after successful login
    /// </summary>
    public bool ResetLockoutOnSuccess { get; set; } = true;
    
    /// <summary>
    /// Enable automatic IP blocking for persistent attackers
    /// </summary>
    public bool EnableAutomaticIPBlocking { get; set; } = true;
    
    /// <summary>
    /// IP block duration for persistent attackers
    /// </summary>
    public TimeSpan AutomaticIPBlockDuration { get; set; } = TimeSpan.FromHours(2);
    
    /// <summary>
    /// Data retention period for security events in days
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Detection window in minutes for analyzing attack patterns
    /// </summary>
    public int DetectionWindowMinutes { get; set; } = 15;
    
    /// <summary>
    /// Velocity window in minutes for rapid attack detection
    /// </summary>
    public int VelocityWindowMinutes { get; set; } = 1;
    
    /// <summary>
    /// CAPTCHA threshold for failed attempts
    /// </summary>
    public int CaptchaThreshold { get; set; } = 3;
}

public class BruteForceAlertThresholds
{
    /// <summary>
    /// Failed logins per minute before alert
    /// </summary>
    public int FailedLoginsPerMinute { get; set; } = 10;
    
    /// <summary>
    /// Account lockouts per hour before alert
    /// </summary>
    public int AccountLockoutsPerHour { get; set; } = 5;
    
    /// <summary>
    /// Distributed attacks per hour before alert
    /// </summary>
    public int DistributedAttacksPerHour { get; set; } = 3;
    
    /// <summary>
    /// Sequential attacks per hour before alert
    /// </summary>
    public int SequentialAttacksPerHour { get; set; } = 5;
    
    /// <summary>
    /// Velocity attacks per hour before alert
    /// </summary>
    public int VelocityAttacksPerHour { get; set; } = 10;
}