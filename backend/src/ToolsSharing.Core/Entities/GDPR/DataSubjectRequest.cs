namespace ToolsSharing.Core.Entities.GDPR;

public class DataSubjectRequest : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DataRequestType RequestType { get; set; }
    public DateTime RequestDate { get; set; }
    public string? RequestDetails { get; set; }
    public DataRequestStatus Status { get; set; } = DataRequestStatus.Pending;
    public DateTime? ResponseDate { get; set; }
    public string? ResponseDetails { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? ProcessedByUserId { get; set; }
    public string? DataExportPath { get; set; }
    public string? VerificationMethod { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
}

public enum DataRequestType
{
    Access,
    Rectification,
    Erasure,
    Portability,
    Restriction,
    Objection
}

public enum DataRequestStatus
{
    Pending,
    InProgress,
    Completed,
    Rejected,
    PartiallyCompleted
}