using System;
using System.Threading.Tasks;
using ToolsSharing.Core.DTOs.Payment;

namespace ToolsSharing.Core.Interfaces;

public interface IPaymentProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    
    // Payment creation
    Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request);
    
    // Payment capture/execution
    Task<CapturePaymentResult> CapturePaymentAsync(CapturePaymentRequest request);
    
    // Payment status
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId);
    
    // Refunds
    Task<RefundResult> RefundPaymentAsync(RefundPaymentRequest request);
    
    // Payouts
    Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request);
    Task<PayoutStatusResult> GetPayoutStatusAsync(string payoutId);
    
    // Webhooks
    Task<WebhookValidationResult> ValidateWebhookAsync(string payload, Dictionary<string, string> headers);
    Task<WebhookProcessResult> ProcessWebhookAsync(string payload);
}