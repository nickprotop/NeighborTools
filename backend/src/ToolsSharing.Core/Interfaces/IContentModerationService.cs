namespace ToolsSharing.Core.Interfaces;

public interface IContentModerationService
{
    /// <summary>
    /// Validates message content for legal compliance and community guidelines
    /// </summary>
    /// <param name="content">The message content to validate</param>
    /// <param name="senderId">The ID of the user sending the message</param>
    /// <returns>Moderation result with approval status and any required modifications</returns>
    Task<ContentModerationResult> ValidateContentAsync(string content, string senderId);
    
    /// <summary>
    /// Reports a message for manual review
    /// </summary>
    /// <param name="messageId">The ID of the message to report</param>
    /// <param name="reporterId">The ID of the user reporting the message</param>
    /// <param name="reason">The reason for reporting</param>
    /// <returns>Success status of the report</returns>
    Task<bool> ReportMessageAsync(Guid messageId, string reporterId, string reason);
    
    /// <summary>
    /// Gets moderation statistics for admin dashboard
    /// </summary>
    /// <returns>Moderation statistics</returns>
    Task<ModerationStatisticsDto> GetModerationStatisticsAsync();
}

public class ContentModerationResult
{
    public bool IsApproved { get; set; }
    public string? ModifiedContent { get; set; }
    public string? ModerationReason { get; set; }
    public ModerationSeverity Severity { get; set; }
    public List<string> Violations { get; set; } = new();
    public bool RequiresManualReview { get; set; }
}

public enum ModerationSeverity
{
    Clean = 0,
    Minor = 1,
    Moderate = 2,
    Severe = 3,
    Critical = 4
}

public class ModerationStatisticsDto
{
    public int TotalMessagesProcessed { get; set; }
    public int ApprovedMessages { get; set; }
    public int ModeratedMessages { get; set; }
    public int PendingReview { get; set; }
    public Dictionary<ModerationSeverity, int> ViolationsBySeverity { get; set; } = new();
    public List<string> CommonViolations { get; set; } = new();
}