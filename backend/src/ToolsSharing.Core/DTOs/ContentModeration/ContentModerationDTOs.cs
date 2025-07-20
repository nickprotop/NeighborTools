namespace ToolsSharing.Core.DTOs.ContentModeration;

public class ContentModerationOptions
{
    public List<string> Models { get; set; } = new();
    public string? Language { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; } = new();
    public bool IncludeDetectionDetails { get; set; } = true;
    public double? Threshold { get; set; }
}

public class ContentModerationResult
{
    public bool IsApproved { get; set; }
    public string? ModifiedContent { get; set; }
    public string? ModerationReason { get; set; }
    public ModerationSeverity Severity { get; set; }
    public List<string> Violations { get; set; } = new();
    public bool RequiresManualReview { get; set; }
    public string? RawResponseJson { get; set; }
    public List<Detection> Detections { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string Provider { get; set; } = "";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class Detection
{
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public double Confidence { get; set; }
    public string? Description { get; set; }
    public BoundingBox? BoundingBox { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class WorkflowModerationResult
{
    public bool Success { get; set; }
    public string WorkflowId { get; set; } = "";
    public Dictionary<string, object> Results { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class WorkflowStep
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public Dictionary<string, object> Output { get; set; } = new();
    public double? ExecutionTime { get; set; }
}

public class ServiceHealthResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "";
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> ServiceInfo { get; set; } = new();
}

public class UsageStatsResult
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public Dictionary<string, int> RequestsByModel { get; set; } = new();
    public Dictionary<string, int> RequestsByType { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCost { get; set; }
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