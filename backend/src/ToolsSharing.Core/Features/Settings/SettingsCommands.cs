namespace ToolsSharing.Core.Features.Settings;

public record GetUserSettingsQuery(string UserId);

public record UpdateUserSettingsCommand(
    string UserId,
    PrivacySettingsDto? Privacy = null,
    NotificationSettingsDto? Notifications = null,
    DisplaySettingsDto? Display = null,
    RentalSettingsDto? Rental = null,
    SecuritySettingsDto? Security = null,
    CommunicationSettingsDto? Communication = null
);

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword
);

// Complete settings DTO
public class UserSettingsDto
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public PrivacySettingsDto Privacy { get; set; } = new();
    public NotificationSettingsDto Notifications { get; set; } = new();
    public DisplaySettingsDto Display { get; set; } = new();
    public RentalSettingsDto Rental { get; set; } = new();
    public SecuritySettingsDto Security { get; set; } = new();
    public CommunicationSettingsDto Communication { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Privacy settings
public class PrivacySettingsDto
{
    public bool ShowProfilePicture { get; set; } = true;
    public bool ShowRealName { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
    public bool ShowPhoneNumber { get; set; } = false;
    public bool ShowEmail { get; set; } = false;
    public bool ShowStatistics { get; set; } = true;
}

// Notification settings
public class NotificationSettingsDto
{
    public bool EmailRentalRequests { get; set; } = true;
    public bool EmailRentalUpdates { get; set; } = true;
    public bool EmailMessages { get; set; } = true;
    public bool EmailMarketing { get; set; } = false;
    public bool EmailSecurityAlerts { get; set; } = true;
    public bool PushMessages { get; set; } = true;
    public bool PushReminders { get; set; } = true;
    public bool PushRentalRequests { get; set; } = true;
    public bool PushRentalUpdates { get; set; } = true;
}

// Display preferences
public class DisplaySettingsDto
{
    public string Theme { get; set; } = "system"; // light, dark, system
    public string Language { get; set; } = "en";
    public string Currency { get; set; } = "USD";
    public string TimeZone { get; set; } = "UTC";
}

// Rental preferences
public class RentalSettingsDto
{
    public bool AutoApproveRentals { get; set; } = false;
    public int RentalLeadTime { get; set; } = 24; // hours
    public bool RequireDeposit { get; set; } = true;
    public decimal DefaultDepositPercentage { get; set; } = 0.20m; // 20%
}

// Security settings
public class SecuritySettingsDto
{
    public bool TwoFactorEnabled { get; set; } = false;
    public bool LoginAlertsEnabled { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
}

// Communication preferences
public class CommunicationSettingsDto
{
    public bool AllowDirectMessages { get; set; } = true;
    public bool AllowRentalInquiries { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;
}