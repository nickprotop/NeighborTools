namespace ToolsSharing.Core.Entities;

public class DisputeEvidence : BaseEntity
{
    public Guid DisputeId { get; set; }
    public Dispute? Dispute { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty; // Path to file storage (local/cloud)
    public string Description { get; set; } = string.Empty;
    
    public string UploadedBy { get; set; } = string.Empty; // User ID
    public User? UploadedByUser { get; set; }
    public DateTime UploadedAt { get; set; }
    
    public bool IsPublic { get; set; } = false; // Whether visible to all parties or admin only
    public string? Tags { get; set; } // JSON array of tags for categorization
}