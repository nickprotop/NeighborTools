namespace ToolsSharing.Core.Entities;

public class UserSettings : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    
    // Privacy Settings
    public bool ShowProfilePicture { get; set; } = true;
    public bool ShowRealName { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
    public bool ShowPhoneNumber { get; set; } = false;
    public bool ShowEmail { get; set; } = false;
    public bool ShowStatistics { get; set; } = true;
    
    // Notification Settings
    public bool EmailRentalRequests { get; set; } = true;
    public bool EmailRentalUpdates { get; set; } = true;
    public bool EmailMessages { get; set; } = true;
    public bool EmailMarketing { get; set; } = false;
    public bool EmailSecurityAlerts { get; set; } = true;
    public bool PushMessages { get; set; } = true;
    public bool PushReminders { get; set; } = true;
    public bool PushRentalRequests { get; set; } = true;
    public bool PushRentalUpdates { get; set; } = true;
    
    // Display Preferences
    public string Theme { get; set; } = "system"; // light, dark, system
    public string Language { get; set; } = "en";
    public string Currency { get; set; } = "USD";
    public string TimeZone { get; set; } = "UTC";
    
    // Rental Preferences
    public bool AutoApproveRentals { get; set; } = false;
    public int RentalLeadTime { get; set; } = 24; // hours
    public bool RequireDeposit { get; set; } = true;
    public decimal DefaultDepositPercentage { get; set; } = 0.20m; // 20%
    
    // Security Settings
    public bool TwoFactorEnabled { get; set; } = false;
    public bool LoginAlertsEnabled { get; set; } = true;
    
    // Communication Preferences
    public bool AllowDirectMessages { get; set; } = true;
    public bool AllowRentalInquiries { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;
    
    // Navigation properties
    public User User { get; set; } = null!;
}