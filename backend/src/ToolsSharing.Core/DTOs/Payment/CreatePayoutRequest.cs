using System;

namespace ToolsSharing.Core.DTOs.Payment;

public class CreatePayoutRequest
{
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Note { get; set; } = string.Empty;
    public string? BatchId { get; set; } // For batch payouts
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class CreatePayoutResult
{
    public bool Success { get; set; }
    public string? PayoutId { get; set; }
    public string? BatchId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class PayoutStatusResult
{
    public bool Success { get; set; }
    public string PayoutId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? RecipientEmail { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}