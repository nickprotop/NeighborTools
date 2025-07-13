namespace ToolsSharing.Core.DTOs.Payment;

public class CapturePaymentRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? PayerId { get; set; } // PayPal payer ID
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public class CapturePaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public decimal CapturedAmount { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}