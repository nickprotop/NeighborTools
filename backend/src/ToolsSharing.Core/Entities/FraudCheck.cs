using System;

namespace ToolsSharing.Core.Entities;

public class FraudCheck : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    public FraudCheckType CheckType { get; set; }
    public FraudRiskLevel RiskLevel { get; set; }
    public decimal RiskScore { get; set; } // 0-100
    
    // Check details
    public string CheckDetails { get; set; } = string.Empty; // JSON details
    public string? TriggeredRules { get; set; } // Comma-separated rule names
    
    // Decision and outcome
    public FraudCheckStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ReviewedBy { get; set; } // Admin user ID
    public DateTime? ReviewedAt { get; set; }
    
    // Actions taken
    public bool PaymentBlocked { get; set; }
    public bool UserFlagged { get; set; }
    public bool AdminNotified { get; set; }
    
    // Context data
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
}

public class SuspiciousActivity : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    
    public SuspiciousActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal RiskScore { get; set; } // 0-100
    
    // Pattern details
    public string PatternData { get; set; } = string.Empty; // JSON with pattern specifics
    public int Frequency { get; set; } // How many times detected
    public DateTime FirstDetectedAt { get; set; }
    public DateTime LastDetectedAt { get; set; }
    
    // Related entities
    public string? RelatedPaymentIds { get; set; } // Comma-separated
    public string? RelatedUserIds { get; set; } // For network analysis
    
    // Status and resolution
    public SuspiciousActivityStatus Status { get; set; }
    public string? InvestigationNotes { get; set; }
    public string? ResolvedBy { get; set; } // Admin user ID
    public DateTime? ResolvedAt { get; set; }
    
    // Automatic actions taken
    public bool UserSuspended { get; set; }
    public bool PaymentsBlocked { get; set; }
    public bool RequiresManualReview { get; set; }
}

public class VelocityLimit : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    
    public VelocityLimitType LimitType { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public decimal AmountLimit { get; set; }
    public int TransactionLimit { get; set; }
    
    // Current usage tracking
    public decimal CurrentAmount { get; set; }
    public int CurrentTransactions { get; set; }
    public DateTime WindowStartTime { get; set; }
    
    // Limit configuration
    public bool IsActive { get; set; } = true;
    public string? CustomReason { get; set; } // Why this limit was set
    public DateTime? ExpiresAt { get; set; } // Temporary limits
}

public enum FraudCheckType
{
    VelocityCheck,
    PatternAnalysis,
    AmountThreshold,
    GeolocationAnomaly,
    DeviceFingerprint,
    BehaviorAnalysis,
    NetworkAnalysis,
    ManualReview
}

public enum FraudRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum FraudCheckStatus
{
    Pending,
    Approved,
    Rejected,
    UnderReview,
    Escalated
}

public enum SuspiciousActivityType
{
    RapidTransactions,
    RoundAmountPattern,
    BackAndForthTransactions,
    UnusualVelocity,
    SuspiciousNetwork,
    MultipleAccounts,
    GeolocationAnomaly,
    DeviceAnomaly,
    PaymentPatternAnomaly,
    StructuringBehavior, // Breaking large amounts into smaller ones
    CircularTransactions,
    HighRiskUser
}

public enum SuspiciousActivityStatus
{
    Active,
    UnderInvestigation,
    FalsePositive,
    Confirmed,
    Resolved
}

public enum VelocityLimitType
{
    DailyAmount,
    WeeklyAmount,
    MonthlyAmount,
    DailyTransactions,
    WeeklyTransactions,
    MonthlyTransactions,
    HourlyAmount,
    HourlyTransactions
}