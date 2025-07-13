using System;
using System.Threading.Tasks;
using ToolsSharing.Core.DTOs.Payment;

namespace ToolsSharing.Core.Interfaces;

public interface IPaymentReceiptService
{
    // Receipt generation
    Task<GenerateReceiptResult> GeneratePaymentReceiptAsync(Guid paymentId);
    Task<GenerateReceiptResult> GeneratePayoutReceiptAsync(Guid payoutId);
    Task<GenerateReceiptResult> GenerateTransactionSummaryAsync(Guid rentalId);
    
    // Receipt retrieval
    Task<PaymentReceiptDto> GetPaymentReceiptAsync(Guid paymentId, string userId);
    Task<PayoutReceiptDto> GetPayoutReceiptAsync(Guid payoutId, string userId);
    Task<TransactionSummaryReceiptDto> GetTransactionSummaryAsync(Guid rentalId, string userId);
    
    // Receipt delivery
    Task<SendReceiptResult> SendPaymentReceiptEmailAsync(Guid paymentId, string recipientEmail);
    Task<SendReceiptResult> SendPayoutReceiptEmailAsync(Guid payoutId, string recipientEmail);
    Task<SendReceiptResult> SendTransactionSummaryEmailAsync(Guid rentalId, string recipientEmail);
    
    // Receipt management
    Task<ReceiptListDto> GetUserReceiptsAsync(string userId, ReceiptFilter? filter = null);
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId, string userId);
    Task<string> GetReceiptDownloadUrlAsync(Guid receiptId, string userId);
    
    // Receipt templates and customization
    Task<ReceiptTemplateDto> GetReceiptTemplateAsync(ReceiptType receiptType);
    Task UpdateReceiptTemplateAsync(ReceiptType receiptType, ReceiptTemplateDto template);
}

public class GenerateReceiptResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? DownloadUrl { get; set; }
}

public class SendReceiptResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public DateTime? SentAt { get; set; }
}

public class ReceiptFilter
{
    public ReceiptType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SearchTerm { get; set; }
}