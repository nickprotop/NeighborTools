using ToolsSharing.Core.DTOs.Dispute;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Service for managing mutual dispute closure requests with industry best practices
/// Implements safeguards, restrictions, and admin oversight for user-initiated dispute resolutions
/// </summary>
public interface IMutualDisputeClosureService
{
    // Core mutual closure operations
    Task<CreateMutualClosureResult> CreateMutualClosureRequestAsync(CreateMutualClosureRequest request, string initiatingUserId);
    Task<RespondToMutualClosureResult> RespondToMutualClosureAsync(RespondToMutualClosureRequest request, string respondingUserId);
    Task<RespondToMutualClosureResult> CancelMutualClosureAsync(CancelMutualClosureRequest request, string cancellingUserId);
    
    // Retrieval operations
    Task<MutualClosureDto?> GetMutualClosureAsync(Guid mutualClosureId, string userId);
    Task<GetMutualClosuresResult> GetUserMutualClosuresAsync(string userId, int page = 1, int pageSize = 20);
    Task<GetMutualClosuresResult> GetDisputeMutualClosuresAsync(Guid disputeId, string userId);
    
    // Eligibility and validation
    Task<MutualClosureEligibilityDto> CheckMutualClosureEligibilityAsync(Guid disputeId, string userId);
    Task<bool> CanUserCreateMutualClosureAsync(Guid disputeId, string userId);
    Task<bool> CanUserRespondToMutualClosureAsync(Guid mutualClosureId, string userId);
    
    // Admin oversight operations
    Task<GetMutualClosuresResult> GetMutualClosuresForAdminAsync(int page = 1, int pageSize = 20, MutualClosureStatus? status = null);
    Task<RespondToMutualClosureResult> AdminReviewMutualClosureAsync(AdminReviewMutualClosureRequest request, string adminUserId);
    Task<MutualClosureStatsDto> GetMutualClosureStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    // Background processing
    Task ProcessExpiredMutualClosuresAsync();
    Task SendMutualClosureRemindersAsync();
    
    // Business rule validation
    Task<List<string>> ValidateBusinessRulesAsync(Guid disputeId, CreateMutualClosureRequest request);
    Task<bool> IsDisputeEligibleForMutualClosureAsync(Dispute dispute);
    Task<decimal> GetMaxAllowedRefundAmountAsync(Guid disputeId);
}

/// <summary>
/// Configuration service for mutual closure business rules and restrictions
/// </summary>
public interface IMutualClosureConfigurationService
{
    // Configuration getters
    decimal GetMaxMutualClosureAmount();
    int GetDefaultExpirationHours();
    int GetMaxExpirationHours();
    int GetMinExpirationHours();
    List<DisputeType> GetEligibleDisputeTypes();
    List<DisputeStatus> GetEligibleDisputeStatuses();
    
    // Business rule checks
    bool RequiresAdminReviewForAmount(decimal amount);
    bool RequiresAdminReviewForDisputeType(DisputeType disputeType);
    bool IsUserEligibleForMutualClosure(string userId);
    int GetMaxActiveMutualClosuresPerUser();
    
    // Fraud and abuse prevention
    bool IsUserInCooldownPeriod(string userId);
    int GetUserMutualClosureCount(string userId, TimeSpan period);
    bool ExceedsVelocityLimits(string userId);
}

/// <summary>
/// Service for sending notifications related to mutual closure
/// </summary>
public interface IMutualClosureNotificationService
{
    // Email notifications
    Task SendMutualClosureRequestNotificationAsync(MutualClosureDto mutualClosure);
    Task SendMutualClosureResponseNotificationAsync(MutualClosureDto mutualClosure, bool accepted);
    Task SendMutualClosureExpiryReminderAsync(MutualClosureDto mutualClosure);
    Task SendMutualClosureExpiredNotificationAsync(MutualClosureDto mutualClosure);
    Task SendMutualClosureCancelledNotificationAsync(MutualClosureDto mutualClosure);
    Task SendAdminReviewRequiredNotificationAsync(MutualClosureDto mutualClosure);
    
    // Admin notifications
    Task NotifyAdminOfHighValueMutualClosureAsync(MutualClosureDto mutualClosure);
    Task NotifyAdminOfSuspiciousActivityAsync(string userId, string reason);
}