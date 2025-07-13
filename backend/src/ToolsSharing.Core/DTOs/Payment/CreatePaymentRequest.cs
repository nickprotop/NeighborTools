using System;

namespace ToolsSharing.Core.DTOs.Payment;

public class CreatePaymentRequest
{
    public Guid RentalId { get; set; }
    public string PayerId { get; set; } = string.Empty;
    public string? PayeeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    // For marketplace scenarios
    public bool IsMarketplacePayment { get; set; }
    public decimal? PlatformFee { get; set; }
    public string? PayeeEmail { get; set; } // For PayPal
}

public class CreatePaymentResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? OrderId { get; set; }
    public string? ApprovalUrl { get; set; } // URL to redirect user for approval
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}