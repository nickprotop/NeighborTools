using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToolsSharing.Core.Entities;

/// <summary>
/// Represents a mutual closure request for a dispute that requires agreement from both parties
/// </summary>
public class MutualDisputeClosure : BaseEntity
{

    [Required]
    public Guid DisputeId { get; set; }

    [ForeignKey("DisputeId")]
    public virtual Dispute Dispute { get; set; } = null!;

    /// <summary>
    /// User who initiated the mutual closure request
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string InitiatedByUserId { get; set; } = string.Empty;

    [ForeignKey("InitiatedByUserId")]
    public virtual User InitiatedByUser { get; set; } = null!;

    /// <summary>
    /// User who needs to respond to the mutual closure request
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string ResponseRequiredFromUserId { get; set; } = string.Empty;

    [ForeignKey("ResponseRequiredFromUserId")]
    public virtual User ResponseRequiredFromUser { get; set; } = null!;

    /// <summary>
    /// Current status of the mutual closure request
    /// </summary>
    [Required]
    public MutualClosureStatus Status { get; set; } = MutualClosureStatus.Pending;

    /// <summary>
    /// Proposed resolution summary
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string ProposedResolution { get; set; } = string.Empty;

    /// <summary>
    /// Detailed explanation of the proposed resolution
    /// </summary>
    [MaxLength(2000)]
    public string? ResolutionDetails { get; set; }

    /// <summary>
    /// Agreed refund amount (if applicable)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? AgreedRefundAmount { get; set; }

    /// <summary>
    /// Which party receives the refund (if applicable)
    /// </summary>
    public RefundRecipient? RefundRecipient { get; set; }

    // CreatedAt inherited from BaseEntity

    /// <summary>
    /// When the other party responded (if they have)
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// When the request expires if no response
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Response from the other party
    /// </summary>
    [MaxLength(1000)]
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected)
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Admin who reviewed this closure (if applicable)
    /// </summary>
    [MaxLength(450)]
    public string? ReviewedByAdminId { get; set; }

    [ForeignKey("ReviewedByAdminId")]
    public virtual User? ReviewedByAdmin { get; set; }

    /// <summary>
    /// When admin reviewed this closure
    /// </summary>
    public DateTime? AdminReviewedAt { get; set; }

    /// <summary>
    /// Admin notes on this closure
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Whether this closure requires payment action
    /// </summary>
    public bool RequiresPaymentAction { get; set; }

    /// <summary>
    /// PayPal or external transaction ID for any refunds processed
    /// </summary>
    [MaxLength(100)]
    public string? RefundTransactionId { get; set; }

    /// <summary>
    /// Audit trail of all actions taken
    /// </summary>
    public virtual ICollection<MutualClosureAuditLog> AuditLogs { get; set; } = new List<MutualClosureAuditLog>();

    /// <summary>
    /// Check if this request has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == MutualClosureStatus.Pending;

    /// <summary>
    /// Check if this request is still actionable
    /// </summary>
    public bool IsActionable => Status == MutualClosureStatus.Pending && !IsExpired;
}

/// <summary>
/// Status of a mutual closure request
/// </summary>
public enum MutualClosureStatus
{
    /// <summary>
    /// Waiting for response from the other party
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Accepted by both parties and dispute closed
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Rejected by the other party
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Expired without response
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Cancelled by the initiator
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Under admin review (for high-value or complex cases)
    /// </summary>
    UnderAdminReview = 5,

    /// <summary>
    /// Blocked by admin (not eligible for mutual closure)
    /// </summary>
    AdminBlocked = 6
}

/// <summary>
/// Who should receive a refund in the mutual closure
/// </summary>
public enum RefundRecipient
{
    /// <summary>
    /// No refund involved
    /// </summary>
    None = 0,

    /// <summary>
    /// Renter receives refund
    /// </summary>
    Renter = 1,

    /// <summary>
    /// Owner receives compensation
    /// </summary>
    Owner = 2,

    /// <summary>
    /// Partial refund to both parties
    /// </summary>
    Both = 3
}

/// <summary>
/// Audit log for mutual closure actions
/// </summary>
public class MutualClosureAuditLog : BaseEntity
{

    [Required]
    public Guid MutualClosureId { get; set; }

    [ForeignKey("MutualClosureId")]
    public virtual MutualDisputeClosure MutualClosure { get; set; } = null!;

    /// <summary>
    /// User who performed the action
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Type of action performed
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Description of the action
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the action
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata { get; set; }

    // CreatedAt inherited from BaseEntity

    /// <summary>
    /// IP address of the user (for security audit)
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the request (for security audit)
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}