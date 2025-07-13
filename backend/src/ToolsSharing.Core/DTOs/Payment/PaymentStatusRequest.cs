using System;

namespace ToolsSharing.Core.DTOs.Payment;

public class PaymentStatusResult
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? PayerEmail { get; set; }
    public string? PayerName { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}