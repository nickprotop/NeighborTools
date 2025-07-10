namespace ToolsSharing.Core.Entities.GDPR;

public class PrivacyPolicyVersion : BaseEntity
{
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
}