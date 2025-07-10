namespace ToolsSharing.Core.Entities.GDPR;

public class DataProcessingLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public string ProcessingPurpose { get; set; } = string.Empty;
    public LegalBasis LegalBasis { get; set; }
    public List<string> DataSources { get; set; } = new();
    public List<string>? DataRecipients { get; set; }
    public string RetentionPeriod { get; set; } = string.Empty;
    public DateTime ProcessingDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

public enum LegalBasis
{
    Consent,
    Contract,
    LegalObligation,
    VitalInterests,
    PublicTask,
    LegitimateInterests
}