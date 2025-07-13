using System;

namespace ToolsSharing.Core.Entities;

public class PaymentSettings : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    
    // Payout preferences
    public PaymentProvider PreferredPayoutMethod { get; set; } = PaymentProvider.PayPal;
    public string? PayPalEmail { get; set; }
    public string? StripeAccountId { get; set; } // For future Stripe Connect
    
    // Commission settings (as tool owner)
    public decimal? CustomCommissionRate { get; set; } // Override platform default
    public bool IsCommissionEnabled { get; set; } = true;
    
    // Payout scheduling
    public PayoutSchedule PayoutSchedule { get; set; } = PayoutSchedule.OnDemand;
    public int? PayoutDayOfWeek { get; set; } // 0-6 for weekly
    public int? PayoutDayOfMonth { get; set; } // 1-28 for monthly
    public decimal MinimumPayoutAmount { get; set; } = 10.00m; // Minimum amount before payout
    
    // Tax information
    public bool TaxInfoProvided { get; set; }
    public string? TaxIdType { get; set; } // SSN, EIN, etc.
    public string? TaxIdLast4 { get; set; } // Last 4 digits only for display
    public string? BusinessName { get; set; }
    public string? BusinessType { get; set; } // Individual, LLC, etc.
    
    // Verification
    public bool IsPayoutVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
    
    // Notifications
    public bool NotifyOnPaymentReceived { get; set; } = true;
    public bool NotifyOnPayoutSent { get; set; } = true;
    public bool NotifyOnPayoutFailed { get; set; } = true;
}

public enum PayoutSchedule
{
    OnDemand,    // Manual request
    Daily,       // Every day
    Weekly,      // Specific day of week
    BiWeekly,    // Every 2 weeks
    Monthly      // Specific day of month
}