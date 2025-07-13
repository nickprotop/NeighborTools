using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IPaymentStatusService
{
    // Payment status explanations
    Task<PaymentStatusExplanationDto> GetPaymentStatusExplanationAsync(Guid paymentId, string userId);
    Task<PaymentTimelineDto> GetPaymentTimelineAsync(Guid paymentId, string userId);
    Task<List<PaymentSummaryDto>> GetUserPaymentsAsync(string userId, PaymentStatusFilter? filter = null);
    
    // Payout timeline and communication
    Task<PayoutTimelineDto> GetPayoutTimelineAsync(Guid rentalId, string ownerId);
    Task<PayoutEstimateDto> GetPayoutEstimateAsync(Guid rentalId, string ownerId);
    Task<List<PayoutSummaryDto>> GetOwnerPayoutsAsync(string ownerId, PayoutStatusFilter? filter = null);
    
    // Communication and notifications
    Task SendPaymentStatusUpdateAsync(Guid paymentId, PaymentStatusChange statusChange);
    Task SendPayoutTimelineUpdateAsync(Guid rentalId, PayoutStatusChange statusChange);
    Task<List<PaymentNotificationDto>> GetPaymentNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkNotificationAsReadAsync(Guid notificationId, string userId);
    
    // Status tracking
    Task<PaymentHealthDto> GetPaymentHealthOverviewAsync(string userId);
    Task<PayoutScheduleDto> GetPayoutScheduleAsync(string ownerId);
    Task UpdatePayoutPreferencesAsync(string ownerId, UpdatePayoutPreferencesRequest request);
}

public class PaymentStatusFilter
{
    public PaymentStatus? Status { get; set; }
    public PaymentType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class PayoutStatusFilter
{
    public PayoutStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class PaymentStatusChange
{
    public PaymentStatus OldStatus { get; set; }
    public PaymentStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? AdditionalInfo { get; set; }
}

public class PayoutStatusChange
{
    public PayoutStatus OldStatus { get; set; }
    public PayoutStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public DateTime? EstimatedDate { get; set; }
    public string? AdditionalInfo { get; set; }
}

public class UpdatePayoutPreferencesRequest
{
    public PayoutSchedule PayoutSchedule { get; set; }
    public int? PayoutDayOfWeek { get; set; }
    public int? PayoutDayOfMonth { get; set; }
    public decimal MinimumPayoutAmount { get; set; }
    public PaymentProvider PreferredPayoutMethod { get; set; }
}