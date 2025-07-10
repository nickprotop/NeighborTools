namespace ToolsSharing.Core.Entities.GDPR;

public class CookieConsent : BaseEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public CookieCategory CookieCategory { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }

    // Navigation properties
    public User? User { get; set; }
}

public enum CookieCategory
{
    Essential,
    Functional,
    Analytics,
    Marketing
}