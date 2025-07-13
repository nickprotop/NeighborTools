using System;
using System.Collections.Generic;

namespace ToolsSharing.Core.Entities;

public class Transaction : BaseEntity
{
    public Guid RentalId { get; set; }
    public Rental? Rental { get; set; }
    
    // Financial breakdown
    public decimal RentalAmount { get; set; }      // Base rental cost
    public decimal SecurityDeposit { get; set; }   // Security deposit amount
    public decimal CommissionRate { get; set; }    // Commission percentage (e.g., 0.10 for 10%)
    public decimal CommissionAmount { get; set; }  // Calculated commission
    public decimal TotalAmount { get; set; }       // Total paid by renter
    public decimal OwnerPayoutAmount { get; set; } // Amount to be paid to owner
    
    public string Currency { get; set; } = "USD";
    
    public TransactionStatus Status { get; set; }
    
    // Tracking dates
    public DateTime? PaymentCompletedAt { get; set; }
    public DateTime? PayoutScheduledAt { get; set; }
    public DateTime? PayoutCompletedAt { get; set; }
    public DateTime? DepositRefundedAt { get; set; }
    
    // Dispute/Issue tracking
    public bool HasDispute { get; set; }
    public string? DisputeReason { get; set; }
    public DateTime? DisputeOpenedAt { get; set; }
    public DateTime? DisputeResolvedAt { get; set; }
    
    // Relations to payments
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum TransactionStatus
{
    Pending,              // Initial state
    PaymentProcessing,    // Payment being processed
    PaymentCompleted,     // Payment received from renter
    Active,              // Rental is active
    Completed,           // Rental completed successfully
    PayoutPending,       // Waiting to payout to owner
    PayoutCompleted,     // Owner has been paid
    Disputed,            // Under dispute
    Cancelled,           // Transaction cancelled
    Refunded,            // Refunded to renter
    UnderReview          // Under fraud/security review
}