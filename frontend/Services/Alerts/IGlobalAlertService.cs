using frontend.Models.Alerts;

namespace frontend.Services.Alerts;

/// <summary>
/// Main service for managing global alerts across the application
/// </summary>
public interface IGlobalAlertService : IDisposable
{
    /// <summary>
    /// Event fired when the combined alert state changes
    /// </summary>
    event EventHandler<GlobalAlertsChangedEventArgs>? GlobalAlertsChanged;
    
    /// <summary>
    /// Register an alert provider with the service
    /// </summary>
    Task RegisterProviderAsync(IGlobalAlertProvider provider);
    
    /// <summary>
    /// Unregister an alert provider
    /// </summary>
    Task UnregisterProviderAsync(string providerId);
    
    /// <summary>
    /// Get all active alerts across all providers, ordered by priority
    /// </summary>
    Task<List<AlertItem>> GetAllActiveAlertsAsync();
    
    /// <summary>
    /// Get alerts filtered by criteria
    /// </summary>
    Task<List<AlertItem>> GetAlertsAsync(AlertPriority? minPriority = null, 
                                        AlertType? alertType = null, 
                                        int? maxCount = null);
    
    /// <summary>
    /// Dismiss an alert by ID
    /// </summary>
    Task<bool> DismissAlertAsync(string alertId);
    
    /// <summary>
    /// Execute an action on an alert
    /// </summary>
    Task<AlertActionResult> ExecuteAlertActionAsync(string alertId, string actionId);
    
    /// <summary>
    /// Force refresh all providers
    /// </summary>
    Task RefreshAllProvidersAsync();
    
    /// <summary>
    /// Get statistics about current alerts
    /// </summary>
    Task<AlertStatistics> GetAlertStatisticsAsync();
    
    /// <summary>
    /// Get registered providers
    /// </summary>
    IReadOnlyList<IGlobalAlertProvider> GetRegisteredProviders();
}

/// <summary>
/// Event arguments for global alert changes
/// </summary>
public class GlobalAlertsChangedEventArgs : EventArgs
{
    public List<AlertItem> AllAlerts { get; set; } = new();
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int UrgentCount { get; set; }
    public int WarningCount { get; set; }
    public string ChangedProviderId { get; set; } = "";
}

/// <summary>
/// Statistics about current alert state
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int UrgentAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }
    public Dictionary<AlertType, int> AlertsByType { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}