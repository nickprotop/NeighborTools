using System;
using System.Collections.Generic;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.DTOs.Payment;

public class PaymentStatusExplanationDto
{
    public Guid PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    
    // Status explanation
    public string StatusTitle { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string UserFriendlyStatus { get; set; } = string.Empty;
    public string NextSteps { get; set; } = string.Empty;
    public bool RequiresAction { get; set; }
    public List<string> PossibleActions { get; set; } = new();
    
    // Timeline information
    public DateTime? EstimatedCompletionDate { get; set; }
    public string? EstimatedCompletionDescription { get; set; }
    public List<PaymentTimelineEventDto> Timeline { get; set; } = new();
    
    // Support information
    public string? SupportMessage { get; set; }
    public bool CanContactSupport { get; set; }
    public string? ContactSupportUrl { get; set; }
    
    // Related information
    public RentalSummaryDto? Rental { get; set; }
    public TransactionSummaryDto? Transaction { get; set; }
}

public class PaymentTimelineDto
{
    public Guid PaymentId { get; set; }
    public string PaymentDescription { get; set; } = string.Empty;
    public List<PaymentTimelineEventDto> Events { get; set; } = new();
    public PaymentTimelineProjectionDto? Projection { get; set; }
}

public class PaymentTimelineEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PaymentEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public PaymentEventStatus Status { get; set; }
    public string? AdditionalInfo { get; set; }
    public bool IsUserVisible { get; set; }
    public string? IconClass { get; set; }
    public string? ColorClass { get; set; }
}

public class PaymentTimelineProjectionDto
{
    public List<PaymentProjectedEventDto> UpcomingEvents { get; set; } = new();
    public string? NextMajorMilestone { get; set; }
    public DateTime? NextMajorMilestoneDate { get; set; }
}

public class PaymentProjectedEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EstimatedDate { get; set; }
    public PaymentEventType EventType { get; set; }
    public bool IsUserActionRequired { get; set; }
}

public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public Guid RentalId { get; set; }
    public string RentalToolName { get; set; } = string.Empty;
    public PaymentType Type { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string UserFriendlyStatus { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public bool RequiresAction { get; set; }
    public bool HasUnreadNotifications { get; set; }
}

public class PayoutTimelineDto
{
    public Guid RentalId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public decimal PayoutAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PayoutStatus CurrentStatus { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    
    // Timeline
    public List<PayoutTimelineEventDto> Events { get; set; } = new();
    public PayoutProjectionDto? Projection { get; set; }
    
    // Payout details
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? PayoutMethod { get; set; }
    public string? PayoutDestination { get; set; }
    public string? TransactionId { get; set; }
}

public class PayoutTimelineEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public PayoutEventType EventType { get; set; }
    public PaymentEventStatus Status { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? IconClass { get; set; }
    public string? ColorClass { get; set; }
}

public class PayoutProjectionDto
{
    public DateTime EstimatedPayoutDate { get; set; }
    public string EstimatedPayoutDescription { get; set; } = string.Empty;
    public List<string> FactorsAffectingPayout { get; set; } = new();
    public string? DelayReason { get; set; }
    public bool IsDelayed { get; set; }
}

public class PayoutEstimateDto
{
    public Guid RentalId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetPayoutAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    
    // Timing
    public DateTime EarliestPayoutDate { get; set; }
    public DateTime EstimatedPayoutDate { get; set; }
    public string PayoutScheduleDescription { get; set; } = string.Empty;
    
    // Breakdown
    public List<PayoutFeeBreakdownDto> FeeBreakdown { get; set; } = new();
    public List<string> PayoutConditions { get; set; } = new();
    public bool IsEligibleForPayout { get; set; }
    public string? IneligibilityReason { get; set; }
}

public class PayoutFeeBreakdownDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsDeduction { get; set; }
    public string? ExplanationUrl { get; set; }
}

public class PayoutSummaryDto
{
    public Guid Id { get; set; }
    public Guid RentalId { get; set; }
    public string RentalToolName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PayoutStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public bool IsDelayed { get; set; }
    public string? DelayReason { get; set; }
}

public class PaymentNotificationDto
{
    public Guid Id { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? RentalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public PaymentNotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public NotificationPriority Priority { get; set; }
}

public class PaymentHealthDto
{
    public int TotalPayments { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPayments { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    
    // Health indicators
    public bool HasPaymentIssues { get; set; }
    public List<PaymentIssueDto> PaymentIssues { get; set; } = new();
    public decimal PaymentSuccessRate { get; set; }
    public int UnreadNotifications { get; set; }
    
    // Quick actions
    public List<QuickActionDto> QuickActions { get; set; } = new();
}

public class PaymentIssueDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PaymentIssueSeverity Severity { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class QuickActionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

public class PayoutScheduleDto
{
    public string OwnerId { get; set; } = string.Empty;
    public PayoutSchedule Schedule { get; set; }
    public int? PayoutDayOfWeek { get; set; }
    public int? PayoutDayOfMonth { get; set; }
    public decimal MinimumPayoutAmount { get; set; }
    public PaymentProvider PreferredPayoutMethod { get; set; }
    
    // Next payout information
    public DateTime? NextScheduledPayout { get; set; }
    public decimal PendingPayoutAmount { get; set; }
    public int PendingPayoutCount { get; set; }
    public bool MeetsMinimumAmount { get; set; }
    
    // Schedule explanation
    public string ScheduleDescription { get; set; } = string.Empty;
    public List<string> PayoutConditions { get; set; } = new();
}

// Supporting enums
public enum PaymentEventType
{
    Created,
    Authorized,
    Captured,
    Failed,
    Refunded,
    Disputed,
    Cancelled,
    UnderReview,
    Approved,
    Rejected
}

public enum PaymentEventStatus
{
    Completed,
    InProgress,
    Pending,
    Failed,
    Cancelled
}

public enum PayoutEventType
{
    PaymentReceived,
    PayoutScheduled,
    PayoutProcessing,
    PayoutSent,
    PayoutFailed,
    PayoutRetried,
    PayoutCancelled
}

public enum PaymentNotificationType
{
    PaymentReceived,
    PaymentFailed,
    PayoutScheduled,
    PayoutSent,
    PayoutFailed,
    DisputeCreated,
    DisputeResolved,
    RefundProcessed,
    ActionRequired
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum PaymentIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

// Supporting DTOs that might be missing
public class RentalSummaryDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
}

public class TransactionSummaryDto
{
    public Guid Id { get; set; }
    public TransactionStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OwnerPayoutAmount { get; set; }
}