using System.ComponentModel;

namespace frontend.Models.Alerts;

/// <summary>
/// Priority levels for global alerts, ordered by importance
/// </summary>
public enum AlertPriority
{
    Info = 1,      // General information, payment setup
    Warning = 2,   // Moderate issues, mild overdue
    Urgent = 3,    // Severe issues, significant overdue
    Critical = 4   // Critical issues, emergency actions needed
}

/// <summary>
/// Types of alerts supported by the system
/// </summary>
public enum AlertType
{
    PaymentSetup,
    OverdueRental,
    SecurityWarning,
    MaintenanceNotice,
    BillingIssue
}

/// <summary>
/// Severity levels that map to MudBlazor Severity enum
/// </summary>
public enum GlobalAlertSeverity
{
    Normal,
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Represents a single alert item to be displayed
/// </summary>
public class AlertItem
{
    public string Id { get; set; } = "";
    public AlertType Type { get; set; }
    public AlertPriority Priority { get; set; }
    public GlobalAlertSeverity Severity { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<AlertAction> Actions { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsDismissible { get; set; } = true;
    public bool ShowProgress { get; set; } = false;
    public double ProgressValue { get; set; } = 0;
    public string ProgressColor { get; set; } = "";
    public string CustomClass { get; set; } = "";
}

/// <summary>
/// Represents an action that can be taken on an alert
/// </summary>
public class AlertAction
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public AlertActionType Type { get; set; }
    public string NavigationUrl { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsFullWidth { get; set; } = false;
    public bool IsPrimary { get; set; } = false;
}

/// <summary>
/// Types of actions that can be performed on alerts
/// </summary>
public enum AlertActionType
{
    Navigate,     // Navigate to a URL
    Execute,      // Execute a method
    Dialog,       // Open a dialog
    External      // External action (API call, etc.)
}

/// <summary>
/// Result of an alert action execution
/// </summary>
public class AlertActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool ShouldRefresh { get; set; } = false;
    public bool ShouldDismiss { get; set; } = false;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Configuration for alert providers
/// </summary>
public class AlertProviderConfig
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAlertsToShow { get; set; } = 10;
    public AlertPriority MinimumPriority { get; set; } = AlertPriority.Info;
    public bool ShowInGlobalMode { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}