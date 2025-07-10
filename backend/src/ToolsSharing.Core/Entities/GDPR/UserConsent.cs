namespace ToolsSharing.Core.Entities.GDPR;

public class UserConsent : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ConsentType ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public string ConsentSource { get; set; } = string.Empty;
    public string ConsentVersion { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime? WithdrawnDate { get; set; }
    public string? WithdrawalReason { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

public enum ConsentType
{
    Cookies,
    Marketing,
    Analytics,
    DataProcessing,
    FinancialData,
    LocationData
}