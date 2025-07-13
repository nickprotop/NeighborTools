using System;
using System.Collections.Generic;

namespace ToolsSharing.Core.Entities;

public class Payout : BaseEntity
{
    public string RecipientId { get; set; } = string.Empty;
    public User? Recipient { get; set; }
    
    // Rental relationship
    public Guid RentalId { get; set; }
    public Rental? Rental { get; set; }
    
    // Owner ID alias for convenience
    public string OwnerId => RecipientId;
    
    public PayoutStatus Status { get; set; }
    public PaymentProvider Provider { get; set; }
    
    // Alias for consistency with other services
    public PaymentProvider PaymentProvider => Provider;
    
    // Financial details
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal PlatformFee { get; set; } // Any platform fees for payout processing
    public decimal NetAmount { get; set; }   // Amount after fees
    
    // Payout method details
    public string? PayoutMethod { get; set; } // e.g., "paypal_email", "bank_account"
    public string? PayoutDestination { get; set; } // e.g., email address, account number (encrypted)
    
    // External references
    public string? ExternalPayoutId { get; set; }
    public string? ExternalBatchId { get; set; } // For batch payouts
    public string? ExternalTransactionId { get; set; } // External transaction reference
    public string? PayPalEmail { get; set; } // PayPal payout destination
    
    // Processing details
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    
    // Related transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    
    // Metadata
    public string? Metadata { get; set; } // JSON for provider-specific data
}

public enum PayoutStatus
{
    Pending,
    Scheduled,
    Processing,
    Completed,
    Failed,
    Cancelled,
    OnHold      // Manual hold for review
}