namespace ToolsSharing.Core.Entities;

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public bool IsScanned { get; set; } = false;
    public bool IsSafe { get; set; } = true;
    public string? ScanResult { get; set; }
    
    // Navigation properties
    public Message Message { get; set; } = null!;
}