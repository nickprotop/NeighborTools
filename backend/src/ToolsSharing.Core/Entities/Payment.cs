using System;

namespace ToolsSharing.Core.Entities;

public class Payment : BaseEntity
{
    public Guid RentalId { get; set; }
    public Rental? Rental { get; set; }
    
    public string PayerId { get; set; } = string.Empty; // User who pays (renter)
    public User? Payer { get; set; }
    
    public string? PayeeId { get; set; } // User who receives (owner) - null for platform fees
    public User? Payee { get; set; }
    
    public PaymentType Type { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentProvider Provider { get; set; }
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    
    // External payment provider references
    public string? ExternalPaymentId { get; set; } // PayPal payment ID
    public string? ExternalOrderId { get; set; } // PayPal order ID
    public string? ExternalPayerId { get; set; } // PayPal payer ID
    
    // Payment details
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    
    // Refund tracking
    public bool IsRefunded { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    
    // Metadata
    public string? Metadata { get; set; } // JSON for provider-specific data
}

public enum PaymentType
{
    RentalPayment,      // Full payment from renter
    OwnerPayout,        // Payout to owner (minus commission)
    PlatformCommission, // Platform commission
    SecurityDeposit,    // Security deposit (held)
    DepositRefund,      // Security deposit refund
    Refund,            // General refund
    Adjustment         // Manual adjustment
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded,
    UnderReview
}

public enum PaymentProvider
{
    PayPal,
    Stripe,        // Future
    CreditCard,    // Future
    BankTransfer,  // Future
    Platform       // Internal platform transactions
}