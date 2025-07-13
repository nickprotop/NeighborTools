using System;
using System.Threading.Tasks;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IPaymentService
{
    // Payment operations
    Task<CreatePaymentResult> InitiateRentalPaymentAsync(Guid rentalId, string userId);
    Task<CapturePaymentResult> CompleteRentalPaymentAsync(string paymentId, string payerId);
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId);
    
    // Refund operations
    Task<RefundResult> RefundRentalAsync(Guid rentalId, decimal amount, string reason);
    Task<RefundResult> RefundSecurityDepositAsync(Guid rentalId);
    
    // Payout operations
    Task<CreatePayoutResult> CreateOwnerPayoutAsync(Guid transactionId);
    Task<PayoutStatusResult> GetPayoutStatusAsync(string payoutId);
    Task ProcessScheduledPayoutsAsync();
    
    // Commission and financial calculations
    decimal CalculateCommission(decimal rentalAmount, string? ownerId = null);
    RentalFinancialBreakdown CalculateRentalFinancials(decimal rentalAmount, decimal securityDeposit, string? ownerId = null);
    
    // Transaction management
    Task<Transaction> GetTransactionByRentalAsync(Guid rentalId);
    Task UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status);
    
    // Payment settings
    Task<PaymentSettings> GetOrCreatePaymentSettingsAsync(string userId);
    Task UpdatePaymentSettingsAsync(string userId, UpdatePaymentSettingsDto settings);
    Task<bool> CanOwnerReceivePaymentsAsync(string ownerId);
    
    // Tool information for calculations
    Task<Tool?> GetToolForCalculationAsync(Guid toolId);
}

public class RentalFinancialBreakdown
{
    public decimal RentalAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TotalPayerAmount { get; set; }
    public decimal OwnerPayoutAmount { get; set; }
}

public class UpdatePaymentSettingsDto
{
    public string? PayPalEmail { get; set; }
    public decimal? CustomCommissionRate { get; set; }
    public PayoutSchedule? PayoutSchedule { get; set; }
    public decimal? MinimumPayoutAmount { get; set; }
    public bool? NotifyOnPaymentReceived { get; set; }
    public bool? NotifyOnPayoutSent { get; set; }
    public bool? NotifyOnPayoutFailed { get; set; }
}