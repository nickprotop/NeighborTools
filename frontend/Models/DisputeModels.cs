namespace frontend.Models;

// Dispute Management Models
public class Dispute
{
    public string Id { get; set; } = string.Empty;
    public string RentalId { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public string InitiatedByName { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? DisputedAmount { get; set; }
    public string? ExternalDisputeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DisputeResolution? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<DisputeMessage> Messages { get; set; } = new();
    public Rental? Rental { get; set; }
}

public class DisputeMessage
{
    public string Id { get; set; } = string.Empty;
    public string DisputeId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsInternal { get; set; }
    public bool IsRead { get; set; }
    public List<string> Attachments { get; set; } = new();
}

public class CreateDisputeRequest
{
    public string RentalId { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? DisputedAmount { get; set; }
    public List<string> Evidence { get; set; } = new();
}

public class CreateDisputeResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dispute? Dispute { get; set; }
}

public class AddDisputeMessageRequest
{
    public string DisputeId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = new();
    public bool IsInternal { get; set; }
}

public class DisputeTimelineEvent
{
    public string Id { get; set; } = string.Empty;
    public string DisputeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DisputeEventType EventType { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = new();
    public bool ActionRequired { get; set; }
    public List<TimelineAction> Actions { get; set; } = new();
    public bool ShowDetails { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TimelineAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool RequiresConfirmation { get; set; }
    public string? ConfirmationMessage { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonClass { get; set; }
    public string? Icon { get; set; }
    public string Color { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ButtonClass { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool Disabled { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? ConfirmationMessage { get; set; }
}

public enum DisputeEventType
{
    Created,
    MessageAdded,
    StatusChanged,
    EvidenceAdded,
    EscalatedToPayPal,
    PayPalUpdate,
    AdminAction,
    Resolved,
    Closed
}

// Fraud Detection Models
public class FraudAlert
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
    public string? RentalId { get; set; }
    public FraudAlertType Type { get; set; }
    public FraudRiskLevel RiskLevel { get; set; }
    public FraudAlertStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public bool RequiresManualReview { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class FraudCheck
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
    public string? RentalId { get; set; }
    public FraudCheckType CheckType { get; set; }
    public FraudCheckResult Result { get; set; }
    public FraudRiskLevel RiskLevel { get; set; }
    public decimal RiskScore { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}

public class UserRiskProfile
{
    public string UserId { get; set; } = string.Empty;
    public FraudRiskLevel OverallRiskLevel { get; set; }
    public decimal RiskScore { get; set; }
    public int FailedPaymentCount { get; set; }
    public int ChargebackCount { get; set; }
    public int DisputeCount { get; set; }
    public int VelocityViolationCount { get; set; }
    public DateTime LastFraudCheck { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public List<FraudAlert> RecentAlerts { get; set; } = new();
}

// Enums
public enum DisputeType
{
    PaymentDispute,
    ServiceDispute,
    Chargeback,
    Refund,
    Damage,
    NoShow,
    Other
}

public enum DisputeStatus
{
    Open,
    InProgress,
    EscalatedToPayPal,
    Resolved,
    Closed,
    Cancelled
}

public enum DisputeCategory
{
    PaymentNotReceived,
    ItemNotReceived,
    ItemNotAsDescribed,
    UnauthorizedPayment,
    DuplicatePayment,
    Damage,
    LateReturn,
    NoShow,
    Other
}

public enum DisputeResolution
{
    RefundToRenter,
    PaymentToOwner,
    PartialRefund,
    NoRefund,
    PayPalResolution,
    Escalated,
    Withdrawn
}

public enum FraudAlertType
{
    VelocityLimit,
    SuspiciousPayment,
    ChargebackRisk,
    AccountTakeover,
    IdentityTheft,
    MoneyLaundering,
    SuspiciousActivity,
    UnusualPattern
}

public enum FraudRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum FraudAlertStatus
{
    New,
    UnderReview,
    Investigated,
    Resolved,
    Dismissed,
    Escalated
}

public enum FraudCheckType
{
    VelocityCheck,
    AmountCheck,
    PaymentPatternCheck,
    DeviceCheck,
    LocationCheck,
    BehaviorCheck,
    BlacklistCheck
}

public enum FraudCheckResult
{
    Pass,
    Warn,
    Block,
    ManualReview
}

// API Response Models
public class DisputeListResponse
{
    public bool Success { get; set; }
    public List<Dispute> Disputes { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class DisputeApiResponse
{
    public bool Success { get; set; }
    public List<Dispute> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class FraudAlertListResponse
{
    public bool Success { get; set; }
    public List<FraudAlert> Alerts { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreviewedCount { get; set; }
}

public class UserRiskProfileResponse
{
    public bool Success { get; set; }
    public UserRiskProfile? Profile { get; set; }
    public string? ErrorMessage { get; set; }
}

// Filter and Search Models
public class DisputeFilter
{
    public DisputeStatus? Status { get; set; }
    public DisputeType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public bool? RequiresAction { get; set; }
}

public class FraudAlertFilter
{
    public FraudAlertStatus? Status { get; set; }
    public FraudRiskLevel? RiskLevel { get; set; }
    public FraudAlertType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? RequiresReview { get; set; }
    public string? UserId { get; set; }
}