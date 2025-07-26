using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Common.Models.EmailNotifications;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailNotificationService _emailService;
    private readonly FraudDetectionConfiguration _config;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(
        ApplicationDbContext context,
        IEmailNotificationService emailService,
        IOptions<FraudDetectionConfiguration> configOptions,
        ILogger<FraudDetectionService> logger)
    {
        _context = context;
        _emailService = emailService;
        _config = configOptions.Value;
        _logger = logger;
    }

    public async Task<FraudCheckResult> CheckPaymentAsync(Payment payment, string? ipAddress = null, string? userAgent = null)
    {
        var result = new FraudCheckResult
        {
            IsApproved = true,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = 0
        };

        try
        {
            var userId = payment.PayerId; // Assuming PayerId is the user making payment
            
            // 1. Check velocity limits
            var velocityCheck = await CheckVelocityLimitsAsync(userId, payment.Amount);
            if (!velocityCheck)
            {
                result.TriggeredRules.Add("VelocityLimit");
                result.RiskScore += 30;
            }

            // 2. Check amount thresholds
            if (payment.Amount >= _config.CriticalRiskAmountThreshold)
            {
                result.TriggeredRules.Add("CriticalAmountThreshold");
                result.RiskScore += 40;
            }
            else if (payment.Amount >= _config.HighRiskAmountThreshold)
            {
                result.TriggeredRules.Add("HighAmountThreshold");
                result.RiskScore += 20;
            }

            // 3. Check for round amount patterns (potential structuring)
            if (IsRoundAmount(payment.Amount))
            {
                result.TriggeredRules.Add("RoundAmountPattern");
                result.RiskScore += 10;
            }

            // 4. Check for back-and-forth transactions
            if (payment.Rental != null)
            {
                var backAndForth = await IsBackAndForthTransactionAsync(userId, payment.Rental.OwnerId, payment.Amount);
                if (backAndForth)
                {
                    result.TriggeredRules.Add("BackAndForthTransaction");
                    result.RiskScore += 25;
                }
            }

            // 5. Check user's overall risk score
            var userRiskScore = await CalculateUserRiskScoreAsync(userId);
            result.RiskScore += userRiskScore * 0.3m; // Weight user risk

            // 6. Check for rapid transactions
            var recentTransactions = await GetRecentTransactionsAsync(userId, _config.RapidTransactionWindow);
            if (recentTransactions.Count >= _config.RapidTransactionThreshold)
            {
                result.TriggeredRules.Add("RapidTransactions");
                result.RiskScore += 20;
            }

            // Determine risk level and actions
            result.RiskLevel = GetRiskLevel(result.RiskScore);
            
            if (result.RiskScore >= _config.AutoBlockRiskScore)
            {
                result.IsApproved = false;
                result.BlockingReason = "High fraud risk detected. Manual review required.";
                result.RequiresManualReview = true;
            }
            else if (result.RiskScore >= _config.ManualReviewRiskScore)
            {
                result.RequiresManualReview = true;
            }

            // Create fraud check record
            var fraudCheck = new FraudCheck
            {
                UserId = userId,
                PaymentId = payment.Id,
                CheckType = FraudCheckType.PatternAnalysis,
                RiskLevel = result.RiskLevel,
                RiskScore = result.RiskScore,
                CheckDetails = JsonSerializer.Serialize(new
                {
                    PaymentAmount = payment.Amount,
                    TriggeredRules = result.TriggeredRules,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CheckTimestamp = DateTime.UtcNow
                }),
                TriggeredRules = string.Join(",", result.TriggeredRules),
                Status = result.IsApproved ? FraudCheckStatus.Approved : FraudCheckStatus.Pending,
                PaymentBlocked = !result.IsApproved,
                UserFlagged = result.RiskScore >= _config.AutoBlockRiskScore,
                AdminNotified = result.RequiresManualReview,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.FraudChecks.Add(fraudCheck);
            await _context.SaveChangesAsync();

            result.FraudCheck = fraudCheck;

            // Send admin notification if required
            if (result.RequiresManualReview)
            {
                await NotifyAdminOfSuspiciousActivityAsync(fraudCheck);
            }

            _logger.LogInformation("Fraud check completed for payment {PaymentId}. Risk Score: {RiskScore}, Approved: {IsApproved}", 
                payment.Id, result.RiskScore, result.IsApproved);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fraud check for payment {PaymentId}", payment.Id);
            
            // In case of error, default to requiring manual review for safety
            result.IsApproved = false;
            result.RequiresManualReview = true;
            result.BlockingReason = "System error during fraud check. Manual review required.";
            
            return result;
        }
    }

    public async Task<FraudCheckResult> CheckUserActivityAsync(string userId, string? activityContext = null)
    {
        var result = new FraudCheckResult
        {
            IsApproved = true,
            RiskLevel = FraudRiskLevel.Low,
            RiskScore = await CalculateUserRiskScoreAsync(userId)
        };

        // Detect suspicious patterns
        var suspiciousActivities = await DetectSuspiciousPatternsAsync(userId);
        result.DetectedActivities = suspiciousActivities;

        if (suspiciousActivities.Any())
        {
            var maxRisk = suspiciousActivities.Max(a => a.RiskScore);
            result.RiskScore = Math.Max(result.RiskScore, maxRisk);
        }

        result.RiskLevel = GetRiskLevel(result.RiskScore);
        
        return result;
    }

    public async Task<bool> CheckVelocityLimitsAsync(string userId, decimal amount)
    {
        var limits = await GetUserVelocityLimitsAsync(userId);
        var now = DateTime.UtcNow;

        foreach (var limit in limits.Where(l => l.IsActive))
        {
            // Reset window if expired
            if (now - limit.WindowStartTime > limit.TimeWindow)
            {
                limit.CurrentAmount = 0;
                limit.CurrentTransactions = 0;
                limit.WindowStartTime = now;
            }

            // Check if adding this amount/transaction would exceed limits
            if (limit.CurrentAmount + amount > limit.AmountLimit || 
                limit.CurrentTransactions + 1 > limit.TransactionLimit)
            {
                _logger.LogWarning("Velocity limit exceeded for user {UserId}. Limit: {LimitType}, Current: {CurrentAmount}/{AmountLimit}, {CurrentTransactions}/{TransactionLimit}", 
                    userId, limit.LimitType, limit.CurrentAmount, limit.AmountLimit, limit.CurrentTransactions, limit.TransactionLimit);
                
                return false;
            }
        }

        return true;
    }

    public async Task UpdateVelocityTrackingAsync(string userId, decimal amount)
    {
        var limits = await GetUserVelocityLimitsAsync(userId);
        var now = DateTime.UtcNow;

        foreach (var limit in limits.Where(l => l.IsActive))
        {
            // Reset window if expired
            if (now - limit.WindowStartTime > limit.TimeWindow)
            {
                limit.CurrentAmount = 0;
                limit.CurrentTransactions = 0;
                limit.WindowStartTime = now;
            }

            limit.CurrentAmount += amount;
            limit.CurrentTransactions++;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<SuspiciousActivity>> DetectSuspiciousPatternsAsync(string userId)
    {
        var activities = new List<SuspiciousActivity>();

        // Check for structuring behavior
        if (await HasStructuringPatternAsync(userId, TimeSpan.FromDays(1)))
        {
            activities.Add(await CreateSuspiciousActivityAsync(userId, SuspiciousActivityType.StructuringBehavior, 
                "Potential structuring detected - multiple transactions under reporting threshold", 70));
        }

        // Check for rapid transactions
        var recentTransactions = await GetRecentTransactionsAsync(userId, _config.RapidTransactionWindow);
        if (recentTransactions.Count >= _config.RapidTransactionThreshold)
        {
            activities.Add(await CreateSuspiciousActivityAsync(userId, SuspiciousActivityType.RapidTransactions, 
                $"Rapid transaction pattern detected - {recentTransactions.Count} transactions in {_config.RapidTransactionWindow.TotalMinutes} minutes", 50));
        }

        return activities;
    }

    public async Task<bool> IsBackAndForthTransactionAsync(string user1Id, string user2Id, decimal amount)
    {
        var timeWindow = DateTime.UtcNow.Subtract(_config.BackAndForthTimeWindow);
        
        // Check for payments between these users in recent timeframe
        var transactions = await _context.Payments
            .Include(p => p.Rental)
            .Where(p => p.ProcessedAt >= timeWindow &&
                       ((p.PayerId == user1Id && p.Rental!.OwnerId == user2Id) ||
                        (p.PayerId == user2Id && p.Rental!.OwnerId == user1Id)))
            .CountAsync();

        return transactions >= _config.BackAndForthThreshold;
    }

    public async Task<bool> HasStructuringPatternAsync(string userId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        var transactions = await _context.Payments
            .Where(p => p.PayerId == userId && p.ProcessedAt >= cutoffTime)
            .Select(p => p.Amount)
            .ToListAsync();

        if (transactions.Count < _config.StructuringThreshold)
            return false;

        var totalAmount = transactions.Sum();
        
        // Check if total amount is above threshold and individual amounts are suspiciously consistent
        if (totalAmount >= _config.StructuringAmountThreshold)
        {
            var avgAmount = totalAmount / transactions.Count;
            var variance = transactions.Average(amt => Math.Abs(amt - avgAmount));
            
            // Suspicious if amounts are very similar (low variance) and under common reporting thresholds
            return variance < (avgAmount * 0.1m) && avgAmount < 1000m;
        }

        return false;
    }

    public async Task<List<string>> GetConnectedUsersAsync(string userId, int depth = 2)
    {
        var connectedUsers = new HashSet<string>();
        var currentLevel = new HashSet<string> { userId };

        for (int i = 0; i < depth; i++)
        {
            var nextLevel = new HashSet<string>();
            
            foreach (var currentUserId in currentLevel)
            {
                // Get users who have had transactions with current user
                var directConnections = await _context.Payments
                    .Include(p => p.Rental)
                    .Where(p => p.PayerId == currentUserId || p.Rental!.OwnerId == currentUserId)
                    .Select(p => p.PayerId == currentUserId ? p.Rental!.OwnerId : p.PayerId)
                    .Distinct()
                    .ToListAsync();

                foreach (var connection in directConnections)
                {
                    if (!connectedUsers.Contains(connection) && connection != userId)
                    {
                        nextLevel.Add(connection);
                        connectedUsers.Add(connection);
                    }
                }
            }
            
            currentLevel = nextLevel;
        }

        return connectedUsers.ToList();
    }

    public async Task<bool> IsCircularTransactionNetworkAsync(List<string> userIds)
    {
        if (userIds.Count < 3) return false;

        var cutoffTime = DateTime.UtcNow.Subtract(_config.CircularTransactionWindow);
        
        // Check for circular payment patterns among these users
        var transactionPairs = new HashSet<(string from, string to)>();
        
        foreach (var payment in await _context.Payments
            .Include(p => p.Rental)
            .Where(p => p.ProcessedAt >= cutoffTime && 
                       userIds.Contains(p.PayerId) && 
                       userIds.Contains(p.Rental!.OwnerId))
            .ToListAsync())
        {
            transactionPairs.Add((payment.PayerId, payment.Rental!.OwnerId));
        }

        // Simple circular detection - check if there's a path from any user back to themselves
        return HasCircularPath(transactionPairs, userIds);
    }

    public async Task<decimal> CalculateUserRiskScoreAsync(string userId)
    {
        decimal riskScore = 0;

        // Recent suspicious activities
        var recentActivities = await _context.SuspiciousActivities
            .Where(a => a.UserId == userId && 
                       a.Status == SuspiciousActivityStatus.Active &&
                       a.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        riskScore += recentActivities.Sum(a => a.RiskScore) * 0.3m;

        // Transaction velocity
        var recentTransactions = await GetRecentTransactionsAsync(userId, TimeSpan.FromDays(1));
        if (recentTransactions.Count > _config.DailyTransactionLimit * 0.8m)
        {
            riskScore += 20;
        }

        // Account age (newer accounts are higher risk)
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            var accountAge = DateTime.UtcNow - user.CreatedAt;
            if (accountAge.TotalDays < 30)
            {
                riskScore += 15;
            }
            else if (accountAge.TotalDays < 90)
            {
                riskScore += 5;
            }
        }

        return Math.Min(riskScore, 100); // Cap at 100
    }

    public async Task<decimal> CalculatePaymentRiskScoreAsync(Payment payment)
    {
        var result = await CheckPaymentAsync(payment);
        return result.RiskScore;
    }

    public async Task<List<FraudCheck>> GetPendingReviewsAsync()
    {
        return await _context.FraudChecks
            .Include(f => f.User)
            .Include(f => f.Payment)
            .Where(f => f.Status == FraudCheckStatus.Pending || f.Status == FraudCheckStatus.UnderReview)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SuspiciousActivity>> GetActiveSuspiciousActivitiesAsync()
    {
        return await _context.SuspiciousActivities
            .Include(s => s.User)
            .Where(s => s.Status == SuspiciousActivityStatus.Active || s.Status == SuspiciousActivityStatus.UnderInvestigation)
            .OrderByDescending(s => s.RiskScore)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task ReviewFraudCheckAsync(Guid fraudCheckId, bool approved, string reviewNotes, string reviewerId)
    {
        var fraudCheck = await _context.FraudChecks.FindAsync(fraudCheckId);
        if (fraudCheck == null) return;

        fraudCheck.Status = approved ? FraudCheckStatus.Approved : FraudCheckStatus.Rejected;
        fraudCheck.ReviewNotes = reviewNotes;
        fraudCheck.ReviewedBy = reviewerId;
        fraudCheck.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Fraud check {FraudCheckId} reviewed by {ReviewerId}. Approved: {Approved}", 
            fraudCheckId, reviewerId, approved);
    }

    public async Task ResolveSuspiciousActivityAsync(Guid activityId, SuspiciousActivityStatus status, string notes, string resolvedBy)
    {
        var activity = await _context.SuspiciousActivities.FindAsync(activityId);
        if (activity == null) return;

        activity.Status = status;
        activity.InvestigationNotes = notes;
        activity.ResolvedBy = resolvedBy;
        activity.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Suspicious activity {ActivityId} resolved by {ResolvedBy}. Status: {Status}", 
            activityId, resolvedBy, status);
    }

    public async Task FlagUserAsync(string userId, string reason, string flaggedBy)
    {
        var activity = await CreateSuspiciousActivityAsync(userId, SuspiciousActivityType.HighRiskUser, 
            $"User flagged: {reason}", 90);
        
        activity.RequiresManualReview = true;
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("User {UserId} flagged by {FlaggedBy}. Reason: {Reason}", userId, flaggedBy, reason);
    }

    public async Task UnflagUserAsync(string userId, string reason, string unflaggedBy)
    {
        var activities = await _context.SuspiciousActivities
            .Where(a => a.UserId == userId && 
                       a.ActivityType == SuspiciousActivityType.HighRiskUser &&
                       a.Status == SuspiciousActivityStatus.Active)
            .ToListAsync();

        foreach (var activity in activities)
        {
            activity.Status = SuspiciousActivityStatus.Resolved;
            activity.InvestigationNotes = $"User unflagged: {reason}";
            activity.ResolvedBy = unflaggedBy;
            activity.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} unflagged by {UnflaggedBy}. Reason: {Reason}", userId, unflaggedBy, reason);
    }

    public async Task<bool> IsUserFlaggedAsync(string userId)
    {
        return await _context.SuspiciousActivities
            .AnyAsync(a => a.UserId == userId && 
                          a.ActivityType == SuspiciousActivityType.HighRiskUser &&
                          a.Status == SuspiciousActivityStatus.Active);
    }

    public async Task SetVelocityLimitAsync(string userId, VelocityLimitType limitType, decimal amountLimit, int transactionLimit, TimeSpan timeWindow)
    {
        var existingLimit = await _context.VelocityLimits
            .FirstOrDefaultAsync(v => v.UserId == userId && v.LimitType == limitType);

        if (existingLimit != null)
        {
            existingLimit.AmountLimit = amountLimit;
            existingLimit.TransactionLimit = transactionLimit;
            existingLimit.TimeWindow = timeWindow;
            existingLimit.IsActive = true;
        }
        else
        {
            var newLimit = new VelocityLimit
            {
                UserId = userId,
                LimitType = limitType,
                AmountLimit = amountLimit,
                TransactionLimit = transactionLimit,
                TimeWindow = timeWindow,
                WindowStartTime = DateTime.UtcNow,
                IsActive = true
            };
            
            _context.VelocityLimits.Add(newLimit);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<VelocityLimit>> GetUserVelocityLimitsAsync(string userId)
    {
        return await _context.VelocityLimits
            .Where(v => v.UserId == userId && v.IsActive)
            .ToListAsync();
    }

    // Private helper methods
    private bool IsRoundAmount(decimal amount)
    {
        var fractionalPart = amount % 1;
        return fractionalPart <= _config.RoundAmountTolerance || 
               fractionalPart >= (1 - _config.RoundAmountTolerance);
    }

    private FraudRiskLevel GetRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            >= 80 => FraudRiskLevel.Critical,
            >= 60 => FraudRiskLevel.High,
            >= 30 => FraudRiskLevel.Medium,
            _ => FraudRiskLevel.Low
        };
    }

    private async Task<List<Payment>> GetRecentTransactionsAsync(string userId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        return await _context.Payments
            .Where(p => p.PayerId == userId && p.ProcessedAt >= cutoffTime)
            .OrderByDescending(p => p.ProcessedAt)
            .ToListAsync();
    }

    private async Task<SuspiciousActivity> CreateSuspiciousActivityAsync(string userId, SuspiciousActivityType activityType, string description, decimal riskScore)
    {
        var activity = new SuspiciousActivity
        {
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            RiskScore = riskScore,
            FirstDetectedAt = DateTime.UtcNow,
            LastDetectedAt = DateTime.UtcNow,
            Frequency = 1,
            Status = SuspiciousActivityStatus.Active,
            RequiresManualReview = riskScore >= _config.ManualReviewRiskScore
        };

        _context.SuspiciousActivities.Add(activity);
        await _context.SaveChangesAsync();
        
        return activity;
    }

    private bool HasCircularPath(HashSet<(string from, string to)> edges, List<string> nodes)
    {
        foreach (var startNode in nodes)
        {
            var visited = new HashSet<string>();
            if (DfsHasCycle(startNode, startNode, edges, visited, new HashSet<string>()))
            {
                return true;
            }
        }
        return false;
    }

    private bool DfsHasCycle(string current, string target, HashSet<(string from, string to)> edges, 
        HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(current))
        {
            return current == target;
        }

        if (visited.Contains(current))
        {
            return false;
        }

        visited.Add(current);
        recursionStack.Add(current);

        foreach (var edge in edges.Where(e => e.from == current))
        {
            if (DfsHasCycle(edge.to, target, edges, visited, recursionStack))
            {
                return true;
            }
        }

        recursionStack.Remove(current);
        return false;
    }

    private async Task NotifyAdminOfSuspiciousActivityAsync(FraudCheck fraudCheck)
    {
        try
        {
            // Get admin emails from configuration or database
            var adminEmails = new[] { "admin@neighbortools.com" }; // This should come from configuration
            
            foreach (var adminEmail in adminEmails)
            {
                var notification = SimpleEmailNotification.Create(
                    adminEmail,
                    "Fraud Alert - Manual Review Required",
                    $@"A high-risk transaction has been detected and requires manual review.

                    Details:
                    - User ID: {fraudCheck.UserId}
                    - Payment ID: {fraudCheck.PaymentId}
                    - Risk Score: {fraudCheck.RiskScore:F1}
                    - Risk Level: {fraudCheck.RiskLevel}
                    - Triggered Rules: {fraudCheck.TriggeredRules}
                    - IP Address: {fraudCheck.IpAddress}
                    - Timestamp: {fraudCheck.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC

                    Please review this transaction in the admin dashboard.",
                    EmailNotificationType.SecurityAlert);

                await _emailService.SendNotificationAsync(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin notification for fraud check {FraudCheckId}", fraudCheck.Id);
        }
    }
}