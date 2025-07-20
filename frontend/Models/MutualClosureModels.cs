using System.ComponentModel.DataAnnotations;

namespace frontend.Models;

// Enums
public enum MutualClosureStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Expired = 3,
    Cancelled = 4,
    UnderAdminReview = 5
}

public enum RefundRecipient
{
    Renter = 0,
    Owner = 1,
    Platform = 2
}

public enum MutualClosureAdminAction
{
    Approve = 1,
    Block = 2,
    RequireReview = 3,
    Override = 4
}

// Request DTOs
public class CreateMutualClosureRequest
{
    [Required]
    public Guid DisputeId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string ProposedResolution { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ResolutionDetails { get; set; }

    [Range(0, 10000)]
    public decimal? AgreedRefundAmount { get; set; }

    public RefundRecipient? RefundRecipient { get; set; }

    public bool RequiresPaymentAction { get; set; }

    /// <summary>
    /// Hours until expiration (default 48 hours)
    /// </summary>
    [Range(24, 168)] // 1 day to 1 week
    public int ExpirationHours { get; set; } = 48;
}

public class RespondToMutualClosureRequest
{
    [Required]
    public Guid MutualClosureId { get; set; }

    [Required]
    public bool? Accept { get; set; }

    [MaxLength(1000)]
    public string? ResponseMessage { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

public class CancelMutualClosureRequest
{
    [Required]
    public Guid MutualClosureId { get; set; }

    [Required]
    [MaxLength(500)]
    public string CancellationReason { get; set; } = string.Empty;
}

// Response DTOs
public class MutualClosureDto
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public string DisputeTitle { get; set; } = string.Empty;
    public string InitiatedByUserId { get; set; } = string.Empty;
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserId { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public MutualClosureStatus Status { get; set; }
    public string ProposedResolution { get; set; } = string.Empty;
    public string? ResolutionDetails { get; set; }
    public decimal? AgreedRefundAmount { get; set; }
    public RefundRecipient? RefundRecipient { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ResponseMessage { get; set; }
    public string? RejectionReason { get; set; }
    public bool RequiresPaymentAction { get; set; }
    public string? RefundTransactionId { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActionable { get; set; }
    public int HoursUntilExpiry { get; set; }
    public List<MutualClosureAuditLogDto> AuditLogs { get; set; } = new();
}

public class MutualClosureAuditLogDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MutualClosureSummaryDto
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public string DisputeTitle { get; set; } = string.Empty;
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string ResponseRequiredFromUserName { get; set; } = string.Empty;
    public MutualClosureStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActionable { get; set; }
    public decimal? AgreedRefundAmount { get; set; }
    public bool RequiresPaymentAction { get; set; }
    public int HoursUntilExpiry { get; set; }
}

// Result DTOs
public class CreateMutualClosureResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? MutualClosureId { get; set; }
    public MutualClosureDto? MutualClosure { get; set; }

    public static CreateMutualClosureResult CreateSuccess(MutualClosureDto mutualClosure)
    {
        return new CreateMutualClosureResult
        {
            Success = true,
            MutualClosureId = mutualClosure.Id,
            MutualClosure = mutualClosure
        };
    }

    public static CreateMutualClosureResult CreateFailure(string errorMessage)
    {
        return new CreateMutualClosureResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class RespondToMutualClosureResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public MutualClosureDto? MutualClosure { get; set; }
    public bool DisputeClosed { get; set; }
    public string? RefundTransactionId { get; set; }

    public static RespondToMutualClosureResult CreateSuccess(MutualClosureDto mutualClosure, bool disputeClosed = false, string? refundTransactionId = null)
    {
        return new RespondToMutualClosureResult
        {
            Success = true,
            MutualClosure = mutualClosure,
            DisputeClosed = disputeClosed,
            RefundTransactionId = refundTransactionId
        };
    }

    public static RespondToMutualClosureResult CreateFailure(string errorMessage)
    {
        return new RespondToMutualClosureResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class GetMutualClosuresResult
{
    public bool Success { get; set; }
    public List<MutualClosureSummaryDto> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public int TotalCount { get; set; }

    public static GetMutualClosuresResult CreateSuccess(List<MutualClosureSummaryDto> data, int totalCount, string? message = null)
    {
        return new GetMutualClosuresResult
        {
            Success = true,
            Data = data,
            TotalCount = totalCount,
            Message = message ?? "Mutual closures retrieved successfully"
        };
    }

    public static GetMutualClosuresResult CreateFailure(string message)
    {
        return new GetMutualClosuresResult
        {
            Success = false,
            Message = message
        };
    }
}

// Statistics DTOs
public class MutualClosureStatistics
{
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalRefundAmount { get; set; }
}

public class MutualClosureEligibilityDto
{
    public bool IsEligible { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<string> Restrictions { get; set; } = new();
    public decimal? MaxRefundAmount { get; set; }
    public bool RequiresAdminReview { get; set; }
}

