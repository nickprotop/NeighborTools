using System.ComponentModel.DataAnnotations;

namespace frontend.Models;

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
    [StringLength(20)]
    public string Theme { get; set; } = "system"; // light, dark, system
    
    [StringLength(10)]
    public string Language { get; set; } = "en";
    
    [StringLength(10)]
    public string Currency { get; set; } = "USD";
    
    [StringLength(100)]
    public string TimeZone { get; set; } = "UTC";
}

// Rental preferences
public class RentalSettingsDto
{
    public bool AutoApproveRentals { get; set; } = false;
    
    [Range(1, 168)] // 1 hour to 1 week
    public int RentalLeadTime { get; set; } = 24; // hours
    
    public bool RequireDeposit { get; set; } = true;
    
    [Range(0.0, 1.0)] // 0% to 100%
    public decimal DefaultDepositPercentage { get; set; } = 0.20m; // 20%
}

// Security settings
public class SecuritySettingsDto
{
    public bool TwoFactorEnabled { get; set; } = false;
    public bool LoginAlertsEnabled { get; set; } = true;
    
    [Range(30, 1440)] // 30 minutes to 24 hours
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
}

// Communication preferences
public class CommunicationSettingsDto
{
    public bool AllowDirectMessages { get; set; } = true;
    public bool AllowRentalInquiries { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;
}

// Request models for API calls
public class UpdateUserSettingsRequest
{
    public PrivacySettingsDto? Privacy { get; set; }
    public NotificationSettingsDto? Notifications { get; set; }
    public DisplaySettingsDto? Display { get; set; }
    public RentalSettingsDto? Rental { get; set; }
    public SecuritySettingsDto? Security { get; set; }
    public CommunicationSettingsDto? Communication { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
        ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, and number")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Form models for UI binding
public class PasswordForm
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Theme options for dropdown
public static class ThemeOptions
{
    public static readonly List<(string Value, string Display)> Options = new()
    {
        ("light", "Light Theme"),
        ("dark", "Dark Theme"),
        ("system", "System Default")
    };
}

// Language options for dropdown
public static class LanguageOptions
{
    public static readonly List<(string Value, string Display)> Options = new()
    {
        ("en", "English"),
        ("es", "Español"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("it", "Italiano")
    };
}

// Currency options for dropdown
public static class CurrencyOptions
{
    public static readonly List<(string Value, string Display)> Options = new()
    {
        ("USD", "US Dollar ($)"),
        ("EUR", "Euro (€)"),
        ("GBP", "British Pound (£)"),
        ("CAD", "Canadian Dollar (C$)"),
        ("AUD", "Australian Dollar (A$)")
    };
}

// Common timezone options
public static class TimezoneOptions
{
    public static readonly List<(string Value, string Display)> Options = new()
    {
        ("UTC", "UTC (Coordinated Universal Time)"),
        ("America/New_York", "Eastern Time (ET)"),
        ("America/Chicago", "Central Time (CT)"),
        ("America/Denver", "Mountain Time (MT)"),
        ("America/Los_Angeles", "Pacific Time (PT)"),
        ("Europe/London", "London (GMT/BST)"),
        ("Europe/Paris", "Paris (CET/CEST)"),
        ("Europe/Berlin", "Berlin (CET/CEST)"),
        ("Asia/Tokyo", "Tokyo (JST)"),
        ("Australia/Sydney", "Sydney (AEST/AEDT)")
    };
}