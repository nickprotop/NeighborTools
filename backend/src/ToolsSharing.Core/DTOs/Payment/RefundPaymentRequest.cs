namespace ToolsSharing.Core.DTOs.Payment;

public class RefundPaymentRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsPartialRefund { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}