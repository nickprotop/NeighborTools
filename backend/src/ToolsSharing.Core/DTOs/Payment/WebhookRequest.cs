namespace ToolsSharing.Core.DTOs.Payment;

public class WebhookValidationResult
{
    public bool IsValid { get; set; }
    public string? EventType { get; set; }
    public string? EventId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WebhookProcessResult
{
    public bool Success { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> ProcessedData { get; set; } = new();
}