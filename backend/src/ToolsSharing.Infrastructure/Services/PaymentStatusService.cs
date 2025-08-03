using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class PaymentStatusService : IPaymentStatusService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<PaymentStatusService> _logger;

    public PaymentStatusService(
        ApplicationDbContext context,
        IEmailNotificationService emailService,
        ILogger<PaymentStatusService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PaymentStatusExplanationDto> GetPaymentStatusExplanationAsync(Guid paymentId, string userId)
    {
        var payment = await _context.Payments
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Owner)
            .Include(p => p.Rental)
                .ThenInclude(r => r.Renter)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found");

        // Verify user access
        if (payment.PayerId != userId && payment.Rental!.OwnerId != userId)
            throw new UnauthorizedAccessException("Access denied");

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == payment.RentalId);

        return new PaymentStatusExplanationDto
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            Type = payment.Type,
            Amount = payment.Amount,
            Currency = payment.Currency,
            StatusTitle = GetStatusTitle(payment.Status),
            StatusDescription = GetStatusDescription(payment.Status, payment.Type),
            UserFriendlyStatus = GetUserFriendlyStatus(payment.Status),
            NextSteps = GetNextSteps(payment.Status, payment.Type, userId == payment.PayerId),
            RequiresAction = RequiresUserAction(payment.Status),
            PossibleActions = GetPossibleActions(payment.Status, payment.Type, userId == payment.PayerId),
            EstimatedCompletionDate = GetEstimatedCompletionDate(payment.Status),
            EstimatedCompletionDescription = GetEstimatedCompletionDescription(payment.Status),
            Timeline = await GetPaymentTimelineEventsAsync(payment),
            SupportMessage = GetSupportMessage(payment.Status),
            CanContactSupport = CanContactSupport(payment.Status),
            ContactSupportUrl = "/support/contact",
            Rental = payment.Rental != null ? new RentalSummaryDto
            {
                Id = payment.Rental.Id,
                ToolName = payment.Rental.Tool!.Name,
                StartDate = payment.Rental.StartDate,
                EndDate = payment.Rental.EndDate,
                TotalCost = payment.Rental.TotalCost
            } : null,
            Transaction = transaction != null ? new TransactionSummaryDto
            {
                Id = transaction.Id,
                Status = transaction.Status,
                TotalAmount = transaction.TotalAmount,
                OwnerPayoutAmount = transaction.OwnerPayoutAmount
            } : null
        };
    }

    public async Task<PaymentTimelineDto> GetPaymentTimelineAsync(Guid paymentId, string userId)
    {
        var payment = await _context.Payments
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found");

        if (payment.PayerId != userId && payment.Rental!.OwnerId != userId)
            throw new UnauthorizedAccessException("Access denied");

        var events = await GetPaymentTimelineEventsAsync(payment);
        var projection = await GetPaymentTimelineProjectionAsync(payment);

        return new PaymentTimelineDto
        {
            PaymentId = payment.Id,
            PaymentDescription = $"{payment.Type} for {payment.Rental!.Tool!.Name}",
            Events = events,
            Projection = projection
        };
    }

    public async Task<List<PaymentSummaryDto>> GetUserPaymentsAsync(string userId, PaymentStatusFilter? filter = null)
    {
        var query = _context.Payments
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Where(p => p.PayerId == userId || p.Rental!.OwnerId == userId);

        if (filter != null)
        {
            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);
            
            if (filter.Type.HasValue)
                query = query.Where(p => p.Type == filter.Type.Value);
            
            if (filter.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.ToDate.Value);
            
            if (filter.MinAmount.HasValue)
                query = query.Where(p => p.Amount >= filter.MinAmount.Value);
            
            if (filter.MaxAmount.HasValue)
                query = query.Where(p => p.Amount <= filter.MaxAmount.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync();

        return payments.Select(p => new PaymentSummaryDto
        {
            Id = p.Id,
            RentalId = p.RentalId,
            RentalToolName = p.Rental!.Tool!.Name,
            Type = p.Type,
            Status = p.Status,
            Amount = p.Amount,
            Currency = p.Currency,
            CreatedAt = p.CreatedAt,
            ProcessedAt = p.ProcessedAt,
            UserFriendlyStatus = GetUserFriendlyStatus(p.Status),
            StatusDescription = GetStatusDescription(p.Status, p.Type),
            RequiresAction = RequiresUserAction(p.Status),
            HasUnreadNotifications = false // TODO: Implement notification tracking
        }).ToList();
    }

    public async Task<PayoutTimelineDto> GetPayoutTimelineAsync(Guid rentalId, string ownerId)
    {
        var rental = await _context.Rentals
            .Include(r => r.Tool)
            .FirstOrDefaultAsync(r => r.Id == rentalId && r.OwnerId == ownerId);

        if (rental == null)
            throw new ArgumentException("Rental not found or access denied");

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == rentalId);

        var payout = await _context.Payouts
            .FirstOrDefaultAsync(p => p.RentalId == rentalId);

        var events = await GetPayoutTimelineEventsAsync(rental, transaction, payout);
        var projection = GetPayoutProjection(transaction, payout);

        return new PayoutTimelineDto
        {
            RentalId = rental.Id,
            ToolName = rental.Tool!.Name,
            PayoutAmount = transaction?.OwnerPayoutAmount ?? 0,
            Currency = transaction?.Currency ?? "USD",
            CurrentStatus = payout?.Status ?? PayoutStatus.Pending,
            StatusDescription = GetPayoutStatusDescription(payout?.Status ?? PayoutStatus.Pending),
            Events = events,
            Projection = projection,
            ScheduledDate = transaction?.PayoutScheduledAt,
            ProcessedDate = payout?.ProcessedAt,
            PayoutMethod = payout?.PaymentProvider.ToString(),
            PayoutDestination = payout?.PayPalEmail,
            TransactionId = payout?.ExternalTransactionId
        };
    }

    public async Task<PayoutEstimateDto> GetPayoutEstimateAsync(Guid rentalId, string ownerId)
    {
        var rental = await _context.Rentals
            .FirstOrDefaultAsync(r => r.Id == rentalId && r.OwnerId == ownerId);

        if (rental == null)
            throw new ArgumentException("Rental not found or access denied");

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RentalId == rentalId);

        if (transaction == null)
            throw new ArgumentException("Transaction not found");

        var paymentSettings = await _context.PaymentSettings
            .FirstOrDefaultAsync(ps => ps.UserId == ownerId);

        var earliestPayoutDate = CalculateEarliestPayoutDate(transaction);
        var estimatedPayoutDate = CalculateEstimatedPayoutDate(transaction, paymentSettings);

        return new PayoutEstimateDto
        {
            RentalId = rentalId,
            GrossAmount = transaction.RentalAmount,
            CommissionAmount = transaction.CommissionAmount,
            NetPayoutAmount = transaction.OwnerPayoutAmount,
            Currency = transaction.Currency,
            EarliestPayoutDate = earliestPayoutDate,
            EstimatedPayoutDate = estimatedPayoutDate,
            PayoutScheduleDescription = GetPayoutScheduleDescription(paymentSettings),
            FeeBreakdown = GetPayoutFeeBreakdown(transaction),
            PayoutConditions = GetPayoutConditions(transaction, paymentSettings),
            IsEligibleForPayout = IsEligibleForPayout(transaction, paymentSettings),
            IneligibilityReason = GetIneligibilityReason(transaction, paymentSettings)
        };
    }

    public async Task<List<PayoutSummaryDto>> GetOwnerPayoutsAsync(string ownerId, PayoutStatusFilter? filter = null)
    {
        var query = _context.Payouts
            .Include(p => p.Rental)
                .ThenInclude(r => r.Tool)
            .Where(p => p.OwnerId == ownerId);

        if (filter != null)
        {
            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);
            
            if (filter.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.ToDate.Value);
            
            if (filter.MinAmount.HasValue)
                query = query.Where(p => p.Amount >= filter.MinAmount.Value);
            
            if (filter.MaxAmount.HasValue)
                query = query.Where(p => p.Amount <= filter.MaxAmount.Value);
        }

        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync();

        return payouts.Select(p => new PayoutSummaryDto
        {
            Id = p.Id,
            RentalId = p.RentalId,
            RentalToolName = p.Rental!.Tool!.Name,
            Amount = p.Amount,
            Currency = p.Currency,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            ScheduledAt = p.ScheduledAt,
            ProcessedAt = p.ProcessedAt,
            StatusDescription = GetPayoutStatusDescription(p.Status),
            IsDelayed = IsPayoutDelayed(p),
            DelayReason = GetPayoutDelayReason(p)
        }).ToList();
    }

    public Task SendPaymentStatusUpdateAsync(Guid paymentId, PaymentStatusChange statusChange)
    {
        // TODO: Implement notification sending
        return Task.CompletedTask;
    }

    public Task SendPayoutTimelineUpdateAsync(Guid rentalId, PayoutStatusChange statusChange)
    {
        // TODO: Implement notification sending
        return Task.CompletedTask;
    }

    public Task<List<PaymentNotificationDto>> GetPaymentNotificationsAsync(string userId, bool unreadOnly = false)
    {
        // TODO: Implement notification retrieval
        return Task.FromResult(new List<PaymentNotificationDto>());
    }

    public Task MarkNotificationAsReadAsync(Guid notificationId, string userId)
    {
        // TODO: Implement notification marking
        return Task.CompletedTask;
    }

    public async Task<PaymentHealthDto> GetPaymentHealthOverviewAsync(string userId)
    {
        var payments = await _context.Payments
            .Where(p => p.PayerId == userId)
            .ToListAsync();

        var totalPayments = payments.Count;
        var pendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing);
        var failedPayments = payments.Count(p => p.Status == PaymentStatus.Failed);
        var totalAmount = payments.Sum(p => p.Amount);
        var pendingAmount = payments.Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing).Sum(p => p.Amount);

        var successRate = totalPayments > 0 ? (totalPayments - failedPayments) / (decimal)totalPayments * 100 : 100;

        return new PaymentHealthDto
        {
            TotalPayments = totalPayments,
            PendingPayments = pendingPayments,
            FailedPayments = failedPayments,
            TotalAmount = totalAmount,
            PendingAmount = pendingAmount,
            Currency = "USD",
            HasPaymentIssues = failedPayments > 0 || pendingPayments > 5,
            PaymentIssues = GetPaymentIssues(payments),
            PaymentSuccessRate = successRate,
            UnreadNotifications = 0, // TODO: Implement notification tracking
            QuickActions = GetQuickActions(payments)
        };
    }

    public async Task<PayoutScheduleDto> GetPayoutScheduleAsync(string ownerId)
    {
        var paymentSettings = await _context.PaymentSettings
            .FirstOrDefaultAsync(ps => ps.UserId == ownerId);

        var pendingTransactions = await _context.Transactions
            .Include(t => t.Rental)
            .Where(t => t.Rental!.OwnerId == ownerId && 
                       t.Status == TransactionStatus.PaymentCompleted &&
                       t.PayoutCompletedAt == null)
            .ToListAsync();

        var pendingAmount = pendingTransactions.Sum(t => t.OwnerPayoutAmount);
        var nextPayout = CalculateNextScheduledPayout(paymentSettings);

        return new PayoutScheduleDto
        {
            OwnerId = ownerId,
            Schedule = paymentSettings?.PayoutSchedule ?? PayoutSchedule.OnDemand,
            PayoutDayOfWeek = paymentSettings?.PayoutDayOfWeek,
            PayoutDayOfMonth = paymentSettings?.PayoutDayOfMonth,
            MinimumPayoutAmount = paymentSettings?.MinimumPayoutAmount ?? 10.00m,
            PreferredPayoutMethod = paymentSettings?.PreferredPayoutMethod ?? PaymentProvider.PayPal,
            NextScheduledPayout = nextPayout,
            PendingPayoutAmount = pendingAmount,
            PendingPayoutCount = pendingTransactions.Count,
            MeetsMinimumAmount = pendingAmount >= (paymentSettings?.MinimumPayoutAmount ?? 10.00m),
            ScheduleDescription = GetPayoutScheduleDescription(paymentSettings),
            PayoutConditions = GetPayoutConditions(null, paymentSettings)
        };
    }

    public async Task UpdatePayoutPreferencesAsync(string ownerId, UpdatePayoutPreferencesRequest request)
    {
        var paymentSettings = await _context.PaymentSettings
            .FirstOrDefaultAsync(ps => ps.UserId == ownerId);

        if (paymentSettings == null)
        {
            paymentSettings = new PaymentSettings
            {
                UserId = ownerId
            };
            _context.PaymentSettings.Add(paymentSettings);
        }

        paymentSettings.PayoutSchedule = request.PayoutSchedule;
        paymentSettings.PayoutDayOfWeek = request.PayoutDayOfWeek;
        paymentSettings.PayoutDayOfMonth = request.PayoutDayOfMonth;
        paymentSettings.MinimumPayoutAmount = request.MinimumPayoutAmount;
        paymentSettings.PreferredPayoutMethod = request.PreferredPayoutMethod;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated payout preferences for owner {OwnerId}", ownerId);
    }

    // Helper methods
    private string GetStatusTitle(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Payment Pending",
        PaymentStatus.Processing => "Payment Processing",
        PaymentStatus.Completed => "Payment Completed",
        PaymentStatus.Failed => "Payment Failed",
        PaymentStatus.Cancelled => "Payment Cancelled",
        PaymentStatus.Refunded => "Payment Refunded",
        PaymentStatus.PartiallyRefunded => "Partially Refunded",
        PaymentStatus.UnderReview => "Under Security Review",
        _ => "Unknown Status"
    };

    private string GetStatusDescription(PaymentStatus status, PaymentType type) => status switch
    {
        PaymentStatus.Pending => "Your payment is waiting to be processed. This usually takes a few minutes.",
        PaymentStatus.Processing => "Your payment is being processed by our payment provider. Please wait.",
        PaymentStatus.Completed => "Your payment has been successfully processed and the rental is confirmed.",
        PaymentStatus.Failed => "Your payment could not be processed. Please try again or use a different payment method.",
        PaymentStatus.Cancelled => "This payment was cancelled.",
        PaymentStatus.Refunded => "This payment has been refunded to your original payment method.",
        PaymentStatus.PartiallyRefunded => "Part of this payment has been refunded to your original payment method.",
        PaymentStatus.UnderReview => "This payment is under security review. We'll notify you once the review is complete.",
        _ => "Payment status unknown."
    };

    private string GetUserFriendlyStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Waiting",
        PaymentStatus.Processing => "Processing",
        PaymentStatus.Completed => "Completed",
        PaymentStatus.Failed => "Failed",
        PaymentStatus.Cancelled => "Cancelled",
        PaymentStatus.Refunded => "Refunded",
        PaymentStatus.PartiallyRefunded => "Partially Refunded",
        PaymentStatus.UnderReview => "Under Review",
        _ => "Unknown"
    };

    private string GetNextSteps(PaymentStatus status, PaymentType type, bool isPayerView) => status switch
    {
        PaymentStatus.Pending when isPayerView => "Please wait while we process your payment. You'll be notified once it's complete.",
        PaymentStatus.Processing when isPayerView => "Your payment is being processed. No action needed from you.",
        PaymentStatus.Failed when isPayerView => "Please try making the payment again or contact support for assistance.",
        PaymentStatus.Completed when isPayerView => "Your rental is confirmed! Check your email for receipt and next steps.",
        PaymentStatus.UnderReview when isPayerView => "We're reviewing this payment for security. You'll be notified once complete.",
        PaymentStatus.Completed when !isPayerView => "Payment received! Your payout will be processed according to your schedule.",
        _ => "No action required at this time."
    };

    private bool RequiresUserAction(PaymentStatus status) => status switch
    {
        PaymentStatus.Failed => true,
        _ => false
    };

    private List<string> GetPossibleActions(PaymentStatus status, PaymentType type, bool isPayerView) => status switch
    {
        PaymentStatus.Failed when isPayerView => new List<string> { "Retry Payment", "Contact Support", "Use Different Payment Method" },
        PaymentStatus.Completed => new List<string> { "View Receipt", "Download Invoice" },
        _ => new List<string>()
    };

    private DateTime? GetEstimatedCompletionDate(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => DateTime.UtcNow.AddMinutes(5),
        PaymentStatus.Processing => DateTime.UtcNow.AddMinutes(10),
        PaymentStatus.UnderReview => DateTime.UtcNow.AddHours(24),
        _ => null
    };

    private string? GetEstimatedCompletionDescription(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Usually completes within 5 minutes",
        PaymentStatus.Processing => "Usually completes within 10 minutes",
        PaymentStatus.UnderReview => "Review typically completes within 24 hours",
        _ => null
    };

    private async Task<List<PaymentTimelineEventDto>> GetPaymentTimelineEventsAsync(Payment payment)
    {
        var events = new List<PaymentTimelineEventDto>();

        events.Add(new PaymentTimelineEventDto
        {
            Id = Guid.NewGuid(),
            Title = "Payment Created",
            Description = $"Payment of {payment.Amount:C} initiated",
            EventType = PaymentEventType.Created,
            Timestamp = payment.CreatedAt,
            Status = PaymentEventStatus.Completed,
            IsUserVisible = true,
            IconClass = "fas fa-credit-card",
            ColorClass = "text-primary"
        });

        if (payment.ProcessedAt.HasValue)
        {
            events.Add(new PaymentTimelineEventDto
            {
                Id = Guid.NewGuid(),
                Title = payment.Status == PaymentStatus.Completed ? "Payment Completed" : "Payment Processed",
                Description = $"Payment processed at {payment.ProcessedAt:g}",
                EventType = payment.Status == PaymentStatus.Completed ? PaymentEventType.Captured : PaymentEventType.Failed,
                Timestamp = payment.ProcessedAt.Value,
                Status = payment.Status == PaymentStatus.Completed ? PaymentEventStatus.Completed : PaymentEventStatus.Failed,
                IsUserVisible = true,
                IconClass = payment.Status == PaymentStatus.Completed ? "fas fa-check-circle" : "fas fa-times-circle",
                ColorClass = payment.Status == PaymentStatus.Completed ? "text-success" : "text-danger"
            });
        }

        if (payment.FailedAt.HasValue)
        {
            events.Add(new PaymentTimelineEventDto
            {
                Id = Guid.NewGuid(),
                Title = "Payment Failed",
                Description = payment.FailureReason ?? "Payment could not be processed",
                EventType = PaymentEventType.Failed,
                Timestamp = payment.FailedAt.Value,
                Status = PaymentEventStatus.Failed,
                IsUserVisible = true,
                IconClass = "fas fa-exclamation-triangle",
                ColorClass = "text-danger"
            });
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }

    private async Task<PaymentTimelineProjectionDto?> GetPaymentTimelineProjectionAsync(Payment payment)
    {
        if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Failed)
            return null;

        var upcomingEvents = new List<PaymentProjectedEventDto>();

        if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Processing)
        {
            upcomingEvents.Add(new PaymentProjectedEventDto
            {
                Title = "Payment Completion",
                Description = "Payment will be completed and rental confirmed",
                EstimatedDate = DateTime.UtcNow.AddMinutes(10),
                EventType = PaymentEventType.Captured,
                IsUserActionRequired = false
            });
        }

        return new PaymentTimelineProjectionDto
        {
            UpcomingEvents = upcomingEvents,
            NextMajorMilestone = upcomingEvents.FirstOrDefault()?.Title,
            NextMajorMilestoneDate = upcomingEvents.FirstOrDefault()?.EstimatedDate
        };
    }

    private async Task<List<PayoutTimelineEventDto>> GetPayoutTimelineEventsAsync(Rental rental, Transaction? transaction, Payout? payout)
    {
        var events = new List<PayoutTimelineEventDto>();

        if (transaction?.PaymentCompletedAt.HasValue == true)
        {
            events.Add(new PayoutTimelineEventDto
            {
                Title = "Payment Received",
                Description = $"Payment of {transaction.TotalAmount:C} received from renter",
                Timestamp = transaction.PaymentCompletedAt.Value,
                EventType = PayoutEventType.PaymentReceived,
                Status = PaymentEventStatus.Completed,
                IconClass = "fas fa-money-bill-wave",
                ColorClass = "text-success"
            });
        }

        if (transaction?.PayoutScheduledAt.HasValue == true)
        {
            events.Add(new PayoutTimelineEventDto
            {
                Title = "Payout Scheduled",
                Description = $"Payout of {transaction.OwnerPayoutAmount:C} scheduled for processing",
                Timestamp = transaction.PayoutScheduledAt.Value,
                EventType = PayoutEventType.PayoutScheduled,
                Status = PaymentEventStatus.Completed,
                IconClass = "fas fa-calendar-check",
                ColorClass = "text-info"
            });
        }

        if (payout?.ProcessedAt.HasValue == true)
        {
            var eventType = payout.Status == PayoutStatus.Completed ? PayoutEventType.PayoutSent : PayoutEventType.PayoutFailed;
            var status = payout.Status == PayoutStatus.Completed ? PaymentEventStatus.Completed : PaymentEventStatus.Failed;
            var color = payout.Status == PayoutStatus.Completed ? "text-success" : "text-danger";
            
            events.Add(new PayoutTimelineEventDto
            {
                Title = payout.Status == PayoutStatus.Completed ? "Payout Sent" : "Payout Failed",
                Description = payout.Status == PayoutStatus.Completed 
                    ? $"Payout of {payout.Amount:C} sent to your {payout.PaymentProvider} account"
                    : $"Payout failed: {payout.FailureReason}",
                Timestamp = payout.ProcessedAt.Value,
                EventType = eventType,
                Status = status,
                IconClass = payout.Status == PayoutStatus.Completed ? "fas fa-check-circle" : "fas fa-times-circle",
                ColorClass = color
            });
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }

    private PayoutProjectionDto? GetPayoutProjection(Transaction? transaction, Payout? payout)
    {
        if (payout?.Status == PayoutStatus.Completed || payout?.Status == PayoutStatus.Failed)
            return null;

        var estimatedDate = transaction?.PayoutScheduledAt ?? DateTime.UtcNow.AddDays(1);
        var factors = new List<string>();

        if (transaction?.Status != TransactionStatus.PaymentCompleted)
            factors.Add("Waiting for payment to complete");

        return new PayoutProjectionDto
        {
            EstimatedPayoutDate = estimatedDate,
            EstimatedPayoutDescription = GetEstimatedPayoutDescription(estimatedDate),
            FactorsAffectingPayout = factors,
            IsDelayed = estimatedDate < DateTime.UtcNow,
            DelayReason = estimatedDate < DateTime.UtcNow ? "Processing delay" : null
        };
    }

    private string GetEstimatedPayoutDescription(DateTime estimatedDate)
    {
        var daysFromNow = (estimatedDate - DateTime.UtcNow).Days;
        return daysFromNow switch
        {
            0 => "Today",
            1 => "Tomorrow",
            _ when daysFromNow > 0 => $"In {daysFromNow} days",
            _ => "Overdue"
        };
    }

    private DateTime CalculateEarliestPayoutDate(Transaction transaction)
    {
        // Earliest payout is typically 1 day after payment completion
        return transaction.PaymentCompletedAt?.AddDays(1) ?? DateTime.UtcNow.AddDays(1);
    }

    private DateTime CalculateEstimatedPayoutDate(Transaction transaction, PaymentSettings? settings)
    {
        if (settings?.PayoutSchedule == PayoutSchedule.Daily)
            return CalculateEarliestPayoutDate(transaction);

        // For other schedules, calculate next payout date
        return CalculateNextScheduledPayout(settings) ?? CalculateEarliestPayoutDate(transaction);
    }

    private DateTime? CalculateNextScheduledPayout(PaymentSettings? settings)
    {
        if (settings == null) return null;

        var now = DateTime.UtcNow;
        
        return settings.PayoutSchedule switch
        {
            PayoutSchedule.Daily => now.AddDays(1),
            PayoutSchedule.Weekly => GetNextWeekday(now, settings.PayoutDayOfWeek ?? (int)DayOfWeek.Friday),
            PayoutSchedule.BiWeekly => GetNextWeekday(now, settings.PayoutDayOfWeek ?? (int)DayOfWeek.Friday).AddDays(14),
            PayoutSchedule.Monthly => GetNextMonthlyDate(now, settings.PayoutDayOfMonth ?? 1),
            _ => null
        };
    }

    private DateTime GetNextWeekday(DateTime from, int dayOfWeek)
    {
        var daysUntilTarget = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0) daysUntilTarget = 7; // Next week if today is the target day
        return from.AddDays(daysUntilTarget);
    }

    private DateTime GetNextMonthlyDate(DateTime from, int dayOfMonth)
    {
        var nextMonth = from.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var targetDay = Math.Min(dayOfMonth, daysInMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, targetDay, 0, 0, 0, DateTimeKind.Utc);
    }

    private string GetPayoutScheduleDescription(PaymentSettings? settings) => settings?.PayoutSchedule switch
    {
        PayoutSchedule.Daily => "Payouts are processed daily",
        PayoutSchedule.Weekly => $"Payouts are processed weekly on {(DayOfWeek)(settings.PayoutDayOfWeek ?? 5)}s",
        PayoutSchedule.BiWeekly => $"Payouts are processed every two weeks on {(DayOfWeek)(settings.PayoutDayOfWeek ?? 5)}s",
        PayoutSchedule.Monthly => $"Payouts are processed monthly on the {GetOrdinal(settings.PayoutDayOfMonth ?? 1)}",
        _ => "Payouts are processed on demand"
    };

    private string GetOrdinal(int number)
    {
        var suffix = (number % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            }
        };
        return $"{number}{suffix}";
    }

    private List<PayoutFeeBreakdownDto> GetPayoutFeeBreakdown(Transaction transaction)
    {
        return new List<PayoutFeeBreakdownDto>
        {
            new PayoutFeeBreakdownDto
            {
                Description = "Rental Payment",
                Amount = transaction.RentalAmount,
                IsDeduction = false
            },
            new PayoutFeeBreakdownDto
            {
                Description = $"Platform Commission ({transaction.CommissionRate:P})",
                Amount = transaction.CommissionAmount,
                IsDeduction = true,
                ExplanationUrl = "/help/fees"
            }
        };
    }

    private List<string> GetPayoutConditions(Transaction? transaction, PaymentSettings? settings)
    {
        var conditions = new List<string>();

        if (settings?.MinimumPayoutAmount > 0)
            conditions.Add($"Minimum payout amount: {settings.MinimumPayoutAmount:C}");

        conditions.Add("Payment must be completed by renter");
        conditions.Add("Tool must be returned in good condition");
        
        if (settings?.PayoutSchedule != PayoutSchedule.OnDemand)
            conditions.Add("Processed according to your payout schedule");

        return conditions;
    }

    private bool IsEligibleForPayout(Transaction transaction, PaymentSettings? settings)
    {
        if (transaction.Status != TransactionStatus.PaymentCompleted)
            return false;

        if (settings?.MinimumPayoutAmount > 0 && transaction.OwnerPayoutAmount < settings.MinimumPayoutAmount)
            return false;

        return true;
    }

    private string? GetIneligibilityReason(Transaction transaction, PaymentSettings? settings)
    {
        if (transaction.Status != TransactionStatus.PaymentCompleted)
            return "Payment not yet completed";

        if (settings?.MinimumPayoutAmount > 0 && transaction.OwnerPayoutAmount < settings.MinimumPayoutAmount)
            return $"Amount below minimum payout threshold of {settings.MinimumPayoutAmount:C}";

        return null;
    }

    private string GetPayoutStatusDescription(PayoutStatus status) => status switch
    {
        PayoutStatus.Pending => "Payout is pending processing",
        PayoutStatus.Scheduled => "Payout is scheduled for processing",
        PayoutStatus.Processing => "Payout is being processed",
        PayoutStatus.Completed => "Payout has been completed",
        PayoutStatus.Failed => "Payout failed to process",
        PayoutStatus.Cancelled => "Payout was cancelled",
        _ => "Unknown status"
    };

    private bool IsPayoutDelayed(Payout payout)
    {
        if (payout.Status == PayoutStatus.Completed || payout.Status == PayoutStatus.Failed)
            return false;

        return payout.ScheduledAt.HasValue && payout.ScheduledAt.Value < DateTime.UtcNow.AddHours(-24);
    }

    private string? GetPayoutDelayReason(Payout payout)
    {
        if (!IsPayoutDelayed(payout))
            return null;

        return payout.Status switch
        {
            PayoutStatus.Processing => "Processing is taking longer than usual",
            PayoutStatus.Scheduled => "Scheduled payout is overdue",
            _ => "Payout is delayed"
        };
    }

    private string? GetSupportMessage(PaymentStatus status) => status switch
    {
        PaymentStatus.Failed => "Having trouble with payments? Our support team can help you resolve the issue.",
        PaymentStatus.UnderReview => "If you have questions about the security review, please contact our support team.",
        _ => null
    };

    private bool CanContactSupport(PaymentStatus status) => status switch
    {
        PaymentStatus.Failed => true,
        PaymentStatus.UnderReview => true,
        _ => false
    };

    private List<PaymentIssueDto> GetPaymentIssues(List<Payment> payments)
    {
        var issues = new List<PaymentIssueDto>();

        var failedPayments = payments.Where(p => p.Status == PaymentStatus.Failed).ToList();
        if (failedPayments.Any())
        {
            issues.Add(new PaymentIssueDto
            {
                Title = $"{failedPayments.Count} Failed Payment{(failedPayments.Count > 1 ? "s" : "")}",
                Description = "Some of your payments have failed and may need attention",
                Severity = PaymentIssueSeverity.Warning,
                ActionUrl = "/payments?filter=failed",
                ActionText = "View Failed Payments",
                DetectedAt = failedPayments.Max(p => p.FailedAt ?? p.CreatedAt)
            });
        }

        var pendingPayments = payments.Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < DateTime.UtcNow.AddHours(-1)).ToList();
        if (pendingPayments.Any())
        {
            issues.Add(new PaymentIssueDto
            {
                Title = $"{pendingPayments.Count} Pending Payment{(pendingPayments.Count > 1 ? "s" : "")}",
                Description = "Some payments have been pending for over an hour",
                Severity = PaymentIssueSeverity.Info,
                ActionUrl = "/payments?filter=pending",
                ActionText = "View Pending Payments",
                DetectedAt = pendingPayments.Max(p => p.CreatedAt)
            });
        }

        return issues;
    }

    private List<QuickActionDto> GetQuickActions(List<Payment> payments)
    {
        var actions = new List<QuickActionDto>();

        if (payments.Any(p => p.Status == PaymentStatus.Failed))
        {
            actions.Add(new QuickActionDto
            {
                Title = "Retry Failed Payments",
                Description = "Attempt to process failed payments again",
                ActionUrl = "/payments/retry",
                ActionText = "Retry Now",
                IconClass = "fas fa-redo"
            });
        }

        actions.Add(new QuickActionDto
        {
            Title = "Payment History",
            Description = "View all your payment transactions",
            ActionUrl = "/payments/history",
            ActionText = "View History",
            IconClass = "fas fa-history"
        });

        return actions;
    }
}