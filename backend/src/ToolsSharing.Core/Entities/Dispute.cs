using System;
using System.Collections.Generic;

namespace ToolsSharing.Core.Entities;

public class Dispute : BaseEntity
{
    public Guid RentalId { get; set; }
    public Rental? Rental { get; set; }
    
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    public string InitiatedBy { get; set; } = string.Empty; // User ID
    public User? Initiator { get; set; }
    
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeCategory Category { get; set; }
    
    // Dispute details
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty; // Alias for Title
    public string Description { get; set; } = string.Empty;
    public decimal? DisputeAmount { get; set; }
    public decimal? DisputedAmount { get; set; } // Alias for DisputeAmount
    public string? Evidence { get; set; } // JSON array of evidence files/links
    
    // Additional properties for service compatibility
    public string InitiatedByName { get; set; } = string.Empty;
    
    // PayPal integration
    public string? ExternalDisputeId { get; set; } // PayPal dispute ID
    public string? ExternalCaseId { get; set; } // PayPal case ID
    public DisputeReason? PayPalReason { get; set; }
    
    // Resolution
    public string? ResolutionNotes { get; set; }
    public string? ResolvedBy { get; set; } // Admin user ID
    public DateTime? ResolvedAt { get; set; }
    public DisputeResolution? Resolution { get; set; }
    public decimal? RefundAmount { get; set; }
    
    // Timeline tracking
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResponseDueDate { get; set; }
    public DateTime? LastActionAt { get; set; }
    
    // Communication
    public ICollection<DisputeMessage> Messages { get; set; } = new List<DisputeMessage>();
    
    // Metadata
    public string? Metadata { get; set; } // JSON for additional data
}

public class DisputeMessage : BaseEntity
{
    public Guid DisputeId { get; set; }
    public Dispute? Dispute { get; set; }
    
    public string FromUserId { get; set; } = string.Empty;
    public User? FromUser { get; set; }
    
    public string? ToUserId { get; set; } // null for public messages
    public User? ToUser { get; set; }
    
    public string Message { get; set; } = string.Empty;
    public string? Attachments { get; set; } // JSON array of attachment URLs
    
    public bool IsFromAdmin { get; set; }
    public bool IsInternal { get; set; } // Only visible to admins
    public bool IsSystemGenerated { get; set; }
    
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
    
    // Additional properties for service compatibility
    public string SenderId { get; set; } = string.Empty; // Alias for FromUserId
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
}

public enum DisputeType
{
    PaymentDispute,      // Payment-related issues
    ItemNotReceived,     // Tool never received
    ItemNotAsDescribed,  // Tool condition/description mismatch
    Damage,              // Damage claims
    Return,              // Return/pickup issues
    Service,             // Service quality issues
    Fraud,               // Fraudulent activity
    Other                // Other disputes
}

public enum DisputeStatus
{
    Open,                // Newly created
    InProgress,          // In progress (alias for UnderReview)
    UnderReview,         // Being reviewed by admin
    AwaitingResponse,    // Waiting for party response
    InMediation,         // Mediation in progress
    Escalated,           // Escalated to PayPal
    EscalatedToPayPal,   // Escalated to PayPal (alias for Escalated)
    Resolved,            // Successfully resolved
    Closed,              // Closed without resolution
    Cancelled,           // Cancelled by user
    PayPalReview         // Under PayPal review
}

public enum DisputeCategory
{
    Billing,
    Service,
    Product,
    Delivery,
    Fraud,
    Authorization,
    Other
}

public enum DisputeReason
{
    // PayPal dispute reasons
    ItemNotReceived,
    ItemSignificantlyNotAsDescribed,
    UnauthorizedTransaction,
    Duplicate,
    Credit,
    Cancelled,
    ProductNotReceived,
    ProductUnacceptable,
    Refund,
    Other
}

public enum DisputeResolution
{
    RefundToBuyer,       // Full refund to renter
    PartialRefund,       // Partial refund
    RefundToSeller,      // Refund to owner
    NoRefund,            // No monetary resolution
    Replacement,         // Item replacement
    StoreCredit,         // Platform credit
    Mediation,           // Third-party mediation
    PayPalDecision       // PayPal made the decision
}