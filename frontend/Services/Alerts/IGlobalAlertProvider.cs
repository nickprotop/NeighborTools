using frontend.Models.Alerts;

namespace frontend.Services.Alerts;

/// <summary>
/// Interface for global alert providers that supply alerts to the global alert system
/// </summary>
public interface IGlobalAlertProvider : IDisposable
{
    /// <summary>
    /// Unique identifier for this alert provider
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Type of alerts this provider handles
    /// </summary>
    AlertType AlertType { get; }
    
    /// <summary>
    /// Configuration for this provider
    /// </summary>
    AlertProviderConfig Configuration { get; }
    
    /// <summary>
    /// Event fired when alerts change
    /// </summary>
    event EventHandler<AlertsChangedEventArgs>? AlertsChanged;
    
    /// <summary>
    /// Initialize the provider
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Get all active alerts from this provider
    /// </summary>
    Task<List<AlertItem>> GetActiveAlertsAsync();
    
    /// <summary>
    /// Dismiss a specific alert
    /// </summary>
    Task<bool> DismissAlertAsync(string alertId);
    
    /// <summary>
    /// Execute an action on an alert
    /// </summary>
    Task<AlertActionResult> ExecuteActionAsync(string alertId, string actionId);
    
    /// <summary>
    /// Force refresh of alerts
    /// </summary>
    Task RefreshAlertsAsync();
    
    /// <summary>
    /// Check if provider is currently enabled
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Enable or disable the provider
    /// </summary>
    Task SetEnabledAsync(bool enabled);
}

/// <summary>
/// Event arguments for alert changes
/// </summary>
public class AlertsChangedEventArgs : EventArgs
{
    public List<AlertItem> UpdatedAlerts { get; set; } = new();
    public List<string> RemovedAlertIds { get; set; } = new();
    public string ProviderId { get; set; } = "";
}