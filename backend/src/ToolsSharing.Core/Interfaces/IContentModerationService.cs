using ToolsSharing.Core.DTOs.ContentModeration;

namespace ToolsSharing.Core.Interfaces;

public interface IContentModerationService
{
    // Legacy message validation methods (maintained for compatibility)
    Task<ContentModerationResult> ValidateContentAsync(string content, string senderId);
    Task<bool> ReportMessageAsync(Guid messageId, string reporterId, string reason);
    Task<ModerationStatisticsDto> GetModerationStatisticsAsync();
    
    // Cloud-based content moderation methods
    Task<ContentModerationResult> ModerateImageAsync(byte[] imageData, string fileName = "", ContentModerationOptions? options = null);
    Task<ContentModerationResult> ModerateImageAsync(string imageUrl, ContentModerationOptions? options = null);
    Task<ContentModerationResult> ModerateTextAsync(string text, ContentModerationOptions? options = null);
    Task<ContentModerationResult> ModerateVideoAsync(byte[] videoData, string fileName = "", ContentModerationOptions? options = null);
    Task<ContentModerationResult> ModerateVideoAsync(string videoUrl, ContentModerationOptions? options = null);
    Task<WorkflowModerationResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters);
    Task<ServiceHealthResult> CheckServiceHealthAsync();
    Task<UsageStatsResult> GetUsageStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

