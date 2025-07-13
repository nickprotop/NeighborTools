using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

public interface IFraudDetectionService
{
    // Main fraud checking methods
    Task<FraudCheckResult> CheckPaymentAsync(Payment payment, string? ipAddress = null, string? userAgent = null);
    Task<FraudCheckResult> CheckUserActivityAsync(string userId, string? activityContext = null);
    
    // Velocity checks
    Task<bool> CheckVelocityLimitsAsync(string userId, decimal amount);
    Task UpdateVelocityTrackingAsync(string userId, decimal amount);
    
    // Pattern detection
    Task<List<SuspiciousActivity>> DetectSuspiciousPatternsAsync(string userId);
    Task<bool> IsBackAndForthTransactionAsync(string user1Id, string user2Id, decimal amount);
    Task<bool> HasStructuringPatternAsync(string userId, TimeSpan timeWindow);
    
    // Network analysis
    Task<List<string>> GetConnectedUsersAsync(string userId, int depth = 2);
    Task<bool> IsCircularTransactionNetworkAsync(List<string> userIds);
    
    // Risk scoring
    Task<decimal> CalculateUserRiskScoreAsync(string userId);
    Task<decimal> CalculatePaymentRiskScoreAsync(Payment payment);
    
    // Administrative methods
    Task<List<FraudCheck>> GetPendingReviewsAsync();
    Task<List<SuspiciousActivity>> GetActiveSuspiciousActivitiesAsync();
    Task ReviewFraudCheckAsync(Guid fraudCheckId, bool approved, string reviewNotes, string reviewerId);
    Task ResolveSuspiciousActivityAsync(Guid activityId, SuspiciousActivityStatus status, string notes, string resolvedBy);
    
    // User management
    Task FlagUserAsync(string userId, string reason, string flaggedBy);
    Task UnflagUserAsync(string userId, string reason, string unflaggedBy);
    Task<bool> IsUserFlaggedAsync(string userId);
    
    // Configuration
    Task SetVelocityLimitAsync(string userId, VelocityLimitType limitType, decimal amountLimit, int transactionLimit, TimeSpan timeWindow);
    Task<List<VelocityLimit>> GetUserVelocityLimitsAsync(string userId);
}

public class FraudCheckResult
{
    public bool IsApproved { get; set; }
    public FraudRiskLevel RiskLevel { get; set; }
    public decimal RiskScore { get; set; }
    public List<string> TriggeredRules { get; set; } = new();
    public string? BlockingReason { get; set; }
    public bool RequiresManualReview { get; set; }
    public FraudCheck? FraudCheck { get; set; }
    public List<SuspiciousActivity> DetectedActivities { get; set; } = new();
}

public class FraudDetectionConfiguration
{
    // Velocity limits (default platform-wide)
    public decimal DailyAmountLimit { get; set; } = 5000m;
    public decimal WeeklyAmountLimit { get; set; } = 15000m;
    public decimal MonthlyAmountLimit { get; set; } = 50000m;
    public int DailyTransactionLimit { get; set; } = 20;
    public int WeeklyTransactionLimit { get; set; } = 100;
    public int MonthlyTransactionLimit { get; set; } = 300;
    
    // Risk thresholds
    public decimal HighRiskAmountThreshold { get; set; } = 2000m;
    public decimal CriticalRiskAmountThreshold { get; set; } = 5000m;
    public int BackAndForthThreshold { get; set; } = 3; // transactions within timeframe
    public TimeSpan BackAndForthTimeWindow { get; set; } = TimeSpan.FromHours(24);
    
    // Pattern detection
    public int RapidTransactionThreshold { get; set; } = 5; // transactions within time window
    public TimeSpan RapidTransactionWindow { get; set; } = TimeSpan.FromMinutes(15);
    public decimal RoundAmountTolerance { get; set; } = 0.01m; // tolerance for "round" amounts
    public int StructuringThreshold { get; set; } = 10; // number of transactions
    public decimal StructuringAmountThreshold { get; set; } = 10000m; // total amount that indicates structuring
    
    // Risk scoring weights
    public decimal VelocityRiskWeight { get; set; } = 0.3m;
    public decimal PatternRiskWeight { get; set; } = 0.25m;
    public decimal AmountRiskWeight { get; set; } = 0.2m;
    public decimal NetworkRiskWeight { get; set; } = 0.15m;
    public decimal BehaviorRiskWeight { get; set; } = 0.1m;
    
    // Auto-blocking thresholds
    public decimal AutoBlockRiskScore { get; set; } = 85m;
    public decimal ManualReviewRiskScore { get; set; } = 60m;
    
    // Network analysis
    public int MaxNetworkDepth { get; set; } = 3;
    public int CircularTransactionMinCount { get; set; } = 3;
    public TimeSpan CircularTransactionWindow { get; set; } = TimeSpan.FromDays(7);
}