namespace frontend.Models;

public class PaymentSettings
{
    public string? PayPalEmail { get; set; } = string.Empty;
    public string PayoutSchedule { get; set; } = "OnDemand";
    public decimal MinimumPayoutAmount { get; set; } = 10.00m;
    public decimal? CustomCommissionRate { get; set; }
    public bool IsCommissionEnabled { get; set; } = true;
    public bool IsPayoutVerified { get; set; } = false;
    public bool NotifyOnPaymentReceived { get; set; } = true;
    public bool NotifyOnPayoutSent { get; set; } = true;
    public bool NotifyOnPayoutFailed { get; set; } = true;
}

public class UpdatePaymentSettingsRequest
{
    public string? PayPalEmail { get; set; }
    public string? PayoutSchedule { get; set; }
    public decimal? MinimumPayoutAmount { get; set; }
    public decimal? CustomCommissionRate { get; set; }
    public bool? NotifyOnPaymentReceived { get; set; }
    public bool? NotifyOnPayoutSent { get; set; }
    public bool? NotifyOnPayoutFailed { get; set; }
}

public class TransactionDetailsResponse
{
    public bool Success { get; set; }
    public Transaction? Transaction { get; set; }
}

public class Transaction
{
    public string Id { get; set; } = string.Empty;
    public string RentalId { get; set; } = string.Empty;
    public decimal RentalAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OwnerPayoutAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public DateTime? PaymentCompletedAt { get; set; }
    public DateTime? PayoutScheduledAt { get; set; }
    public DateTime? PayoutCompletedAt { get; set; }
    public DateTime? DepositRefundedAt { get; set; }
    public Rental? Rental { get; set; }
}

public class Payment
{
    public string Id { get; set; } = string.Empty;
    public string RentalId { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string? PayeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalPaymentId { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? ExternalPayerId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsRefunded { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
}

public class PaymentRequest
{
    public string RentalId { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
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

public enum PayoutSchedule
{
    OnDemand,
    Daily,
    Weekly,
    BiWeekly,
    Monthly
}

public enum PaymentType
{
    RentalPayment,
    OwnerPayout,
    PlatformCommission,
    SecurityDeposit,
    DepositRefund,
    Refund,
    Adjustment
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
    Stripe,
    CreditCard,
    BankTransfer,
    Platform
}

public enum TransactionStatus
{
    Pending,
    PaymentProcessing,
    PaymentCompleted,
    Active,
    Completed,
    PayoutPending,
    PayoutCompleted,
    Disputed,
    Cancelled,
    Refunded
}

// Response models for PaymentService
public class PaymentInitiationResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}

public class PaymentCompletionResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PaymentStatusResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CalculateFeesResponse
{
    public decimal RentalAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal TotalPayerAmount { get; set; }
    public decimal OwnerPayoutAmount { get; set; }
}

public class PaymentSettingsResponse
{
    public bool Success { get; set; }
    public PaymentSettings? Settings { get; set; }
}

public class RefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RefundResponse
{
    public string RefundId { get; set; } = string.Empty;
    public decimal RefundedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CanReceivePaymentsResponse
{
    public bool CanReceivePayments { get; set; }
}

public class RentalCostCalculationResponse
{
    public Guid ToolId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RentalDays { get; set; }
    public string RateType { get; set; } = string.Empty;
    public decimal SelectedRate { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal TotalPayerAmount { get; set; }
    public decimal OwnerPayoutAmount { get; set; }
}