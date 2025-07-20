using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

/// <summary>
/// Configuration service implementing industry best practices for mutual dispute closure
/// Includes safeguards, velocity limits, and fraud prevention measures
/// </summary>
public class MutualClosureConfigurationService : IMutualClosureConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly MutualClosureConfiguration _config;

    public MutualClosureConfigurationService(
        ApplicationDbContext context,
        IOptions<MutualClosureConfiguration> config)
    {
        _context = context;
        _config = config.Value;
    }

    public decimal GetMaxMutualClosureAmount()
    {
        return _config.MaxMutualClosureAmount;
    }

    public int GetDefaultExpirationHours()
    {
        return _config.DefaultExpirationHours;
    }

    public int GetMaxExpirationHours()
    {
        return _config.MaxExpirationHours;
    }

    public int GetMinExpirationHours()
    {
        return _config.MinExpirationHours;
    }

    public List<DisputeType> GetEligibleDisputeTypes()
    {
        return _config.EligibleDisputeTypes;
    }

    public List<DisputeStatus> GetEligibleDisputeStatuses()
    {
        return _config.EligibleDisputeStatuses;
    }

    public bool RequiresAdminReviewForAmount(decimal amount)
    {
        return amount >= _config.AdminReviewThresholdAmount;
    }

    public bool RequiresAdminReviewForDisputeType(DisputeType disputeType)
    {
        return _config.AdminReviewRequiredTypes.Contains(disputeType);
    }

    public bool IsUserEligibleForMutualClosure(string userId)
    {
        try
        {
            // Check if user is in cooldown period
            if (IsUserInCooldownPeriod(userId))
                return false;

            // Check if user has exceeded velocity limits
            if (ExceedsVelocityLimits(userId))
                return false;

            // Check if user has any recent violations or suspensions
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user?.IsDeleted == true) // Using soft delete as suspension
                return false;

            // Check for recent fraud flags or payment issues
            var hasRecentFraudFlags = _context.Set<FraudCheck>()
                .Any(fc => fc.UserId == userId && 
                          fc.CreatedAt >= DateTime.UtcNow.AddDays(-30) &&
                          fc.Status == FraudCheckStatus.Rejected);

            if (hasRecentFraudFlags)
                return false;

            return true;
        }
        catch
        {
            // If there's an error checking eligibility, err on the side of caution
            return false;
        }
    }

    public int GetMaxActiveMutualClosuresPerUser()
    {
        return _config.MaxActiveMutualClosuresPerUser;
    }

    public bool IsUserInCooldownPeriod(string userId)
    {
        try
        {
            var lastRejectedClosure = _context.Set<MutualDisputeClosure>()
                .Where(mc => mc.InitiatedByUserId == userId && 
                           mc.Status == MutualClosureStatus.Rejected)
                .OrderByDescending(mc => mc.RespondedAt)
                .FirstOrDefault();

            if (lastRejectedClosure?.RespondedAt == null)
                return false;

            var cooldownPeriod = TimeSpan.FromHours(_config.CooldownPeriodHours);
            return DateTime.UtcNow - lastRejectedClosure.RespondedAt.Value < cooldownPeriod;
        }
        catch
        {
            return false;
        }
    }

    public int GetUserMutualClosureCount(string userId, TimeSpan period)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(period);
            return _context.Set<MutualDisputeClosure>()
                .Count(mc => mc.InitiatedByUserId == userId && mc.CreatedAt >= cutoffDate);
        }
        catch
        {
            return 0;
        }
    }

    public bool ExceedsVelocityLimits(string userId)
    {
        try
        {
            // Daily limit
            var dailyCount = GetUserMutualClosureCount(userId, TimeSpan.FromDays(1));
            if (dailyCount >= _config.MaxMutualClosuresPerDay)
                return true;

            // Weekly limit
            var weeklyCount = GetUserMutualClosureCount(userId, TimeSpan.FromDays(7));
            if (weeklyCount >= _config.MaxMutualClosuresPerWeek)
                return true;

            // Monthly limit
            var monthlyCount = GetUserMutualClosureCount(userId, TimeSpan.FromDays(30));
            if (monthlyCount >= _config.MaxMutualClosuresPerMonth)
                return true;

            // Check for active mutual closures
            var activeCount = _context.Set<MutualDisputeClosure>()
                .Count(mc => mc.InitiatedByUserId == userId && 
                           mc.Status == MutualClosureStatus.Pending);

            if (activeCount >= _config.MaxActiveMutualClosuresPerUser)
                return true;

            return false;
        }
        catch
        {
            // If there's an error, assume limits are exceeded for safety
            return true;
        }
    }
}

/// <summary>
/// Configuration class for mutual closure settings
/// </summary>
public class MutualClosureConfiguration
{
    /// <summary>
    /// Maximum refund amount that can be processed through mutual closure
    /// </summary>
    public decimal MaxMutualClosureAmount { get; set; } = 500.00m;

    /// <summary>
    /// Amount threshold that requires admin review
    /// </summary>
    public decimal AdminReviewThresholdAmount { get; set; } = 100.00m;

    /// <summary>
    /// Default expiration time for mutual closure requests (hours)
    /// </summary>
    public int DefaultExpirationHours { get; set; } = 48;

    /// <summary>
    /// Minimum expiration time allowed (hours)
    /// </summary>
    public int MinExpirationHours { get; set; } = 24;

    /// <summary>
    /// Maximum expiration time allowed (hours)
    /// </summary>
    public int MaxExpirationHours { get; set; } = 168; // 1 week

    /// <summary>
    /// Dispute types eligible for mutual closure
    /// </summary>
    public List<DisputeType> EligibleDisputeTypes { get; set; } = new()
    {
        DisputeType.Service,
        DisputeType.Damage,
        DisputeType.Return,
        DisputeType.PaymentDispute,
        DisputeType.ItemNotAsDescribed
    };

    /// <summary>
    /// Dispute statuses eligible for mutual closure
    /// </summary>
    public List<DisputeStatus> EligibleDisputeStatuses { get; set; } = new()
    {
        DisputeStatus.Open,
        DisputeStatus.UnderReview
    };

    /// <summary>
    /// Dispute types that always require admin review
    /// </summary>
    public List<DisputeType> AdminReviewRequiredTypes { get; set; } = new()
    {
        DisputeType.PaymentDispute,
        DisputeType.Fraud
    };

    /// <summary>
    /// Maximum number of mutual closure requests per user per day
    /// </summary>
    public int MaxMutualClosuresPerDay { get; set; } = 3;

    /// <summary>
    /// Maximum number of mutual closure requests per user per week
    /// </summary>
    public int MaxMutualClosuresPerWeek { get; set; } = 10;

    /// <summary>
    /// Maximum number of mutual closure requests per user per month
    /// </summary>
    public int MaxMutualClosuresPerMonth { get; set; } = 20;

    /// <summary>
    /// Maximum number of active (pending) mutual closures per user
    /// </summary>
    public int MaxActiveMutualClosuresPerUser { get; set; } = 3;

    /// <summary>
    /// Cooldown period after a rejected mutual closure (hours)
    /// </summary>
    public int CooldownPeriodHours { get; set; } = 24;

    /// <summary>
    /// Whether to automatically send reminder emails before expiration
    /// </summary>
    public bool SendExpiryReminders { get; set; } = true;

    /// <summary>
    /// Hours before expiration to send reminder
    /// </summary>
    public int ReminderHoursBeforeExpiry { get; set; } = 24;

    /// <summary>
    /// Whether to notify admins of high-value mutual closures
    /// </summary>
    public bool NotifyAdminOfHighValue { get; set; } = true;

    /// <summary>
    /// Whether to automatically close disputes when mutual closure is accepted
    /// </summary>
    public bool AutoCloseDisputeOnAcceptance { get; set; } = true;

    /// <summary>
    /// Whether to process refunds automatically when mutual closure is accepted
    /// </summary>
    public bool AutoProcessRefunds { get; set; } = true;

    /// <summary>
    /// Maximum dispute age (days) eligible for mutual closure
    /// </summary>
    public int MaxDisputeAgeDays { get; set; } = 30;

    /// <summary>
    /// Whether to allow mutual closure for disputes with external escalation
    /// </summary>
    public bool AllowWithExternalEscalation { get; set; } = false;
}