using frontend.Models.Alerts;
using System.Collections.Concurrent;

namespace frontend.Services.Alerts;

/// <summary>
/// Main service for managing global alerts across the application
/// </summary>
public class GlobalAlertService : IGlobalAlertService
{
    private readonly ConcurrentDictionary<string, IGlobalAlertProvider> _providers = new();
    private readonly ConcurrentDictionary<string, List<AlertItem>> _providerAlerts = new();
    private readonly Timer _refreshTimer;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private bool _disposed = false;

    public event EventHandler<GlobalAlertsChangedEventArgs>? GlobalAlertsChanged;

    public GlobalAlertService()
    {
        // Refresh every 30 seconds to coordinate all providers
        _refreshTimer = new Timer(async _ => await RefreshAllProvidersAsync(), 
                                 null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task RegisterProviderAsync(IGlobalAlertProvider provider)
    {
        if (_providers.TryAdd(provider.ProviderId, provider))
        {
            // Subscribe to provider changes
            provider.AlertsChanged += OnProviderAlertsChanged;
            
            // Initialize provider
            await provider.InitializeAsync();
            
            // Get initial alerts
            var alerts = await provider.GetActiveAlertsAsync();
            _providerAlerts.AddOrUpdate(provider.ProviderId, alerts, (_, _) => alerts);
            
            // Notify of changes
            await NotifyGlobalAlertsChanged(provider.ProviderId);
        }
    }

    public async Task UnregisterProviderAsync(string providerId)
    {
        if (_providers.TryRemove(providerId, out var provider))
        {
            provider.AlertsChanged -= OnProviderAlertsChanged;
            _providerAlerts.TryRemove(providerId, out _);
            provider.Dispose();
            
            await NotifyGlobalAlertsChanged(providerId);
        }
    }

    public async Task<List<AlertItem>> GetAllActiveAlertsAsync()
    {
        var allAlerts = new List<AlertItem>();
        
        foreach (var providerAlerts in _providerAlerts.Values)
        {
            allAlerts.AddRange(providerAlerts);
        }
        
        // Sort by priority (Critical first) then by creation time
        return allAlerts
            .OrderByDescending(a => (int)a.Priority)
            .ThenByDescending(a => a.CreatedAt)
            .ToList();
    }

    public async Task<List<AlertItem>> GetAlertsAsync(AlertPriority? minPriority = null, 
                                                     AlertType? alertType = null, 
                                                     int? maxCount = null)
    {
        var alerts = await GetAllActiveAlertsAsync();
        
        if (minPriority.HasValue)
        {
            alerts = alerts.Where(a => a.Priority >= minPriority.Value).ToList();
        }
        
        if (alertType.HasValue)
        {
            alerts = alerts.Where(a => a.Type == alertType.Value).ToList();
        }
        
        if (maxCount.HasValue)
        {
            alerts = alerts.Take(maxCount.Value).ToList();
        }
        
        return alerts;
    }

    public async Task<bool> DismissAlertAsync(string alertId)
    {
        // Find which provider owns this alert
        foreach (var provider in _providers.Values)
        {
            var providerAlerts = _providerAlerts.GetValueOrDefault(provider.ProviderId, new List<AlertItem>());
            if (providerAlerts.Any(a => a.Id == alertId))
            {
                var result = await provider.DismissAlertAsync(alertId);
                if (result)
                {
                    // Refresh provider alerts
                    var updatedAlerts = await provider.GetActiveAlertsAsync();
                    _providerAlerts.AddOrUpdate(provider.ProviderId, updatedAlerts, (_, _) => updatedAlerts);
                    
                    await NotifyGlobalAlertsChanged(provider.ProviderId);
                }
                return result;
            }
        }
        
        return false;
    }

    public async Task<AlertActionResult> ExecuteAlertActionAsync(string alertId, string actionId)
    {
        // Find which provider owns this alert
        foreach (var provider in _providers.Values)
        {
            var providerAlerts = _providerAlerts.GetValueOrDefault(provider.ProviderId, new List<AlertItem>());
            if (providerAlerts.Any(a => a.Id == alertId))
            {
                var result = await provider.ExecuteActionAsync(alertId, actionId);
                
                if (result.ShouldRefresh)
                {
                    // Refresh provider alerts
                    var updatedAlerts = await provider.GetActiveAlertsAsync();
                    _providerAlerts.AddOrUpdate(provider.ProviderId, updatedAlerts, (_, _) => updatedAlerts);
                    
                    await NotifyGlobalAlertsChanged(provider.ProviderId);
                }
                
                return result;
            }
        }
        
        return new AlertActionResult { Success = false, Message = "Alert not found" };
    }

    public async Task RefreshAllProvidersAsync()
    {
        if (!await _refreshSemaphore.WaitAsync(100)) // Don't block if already refreshing
            return;
            
        try
        {
            var refreshTasks = _providers.Values
                .Where(p => p.IsEnabled)
                .Select(async provider =>
                {
                    try
                    {
                        await provider.RefreshAlertsAsync();
                        var alerts = await provider.GetActiveAlertsAsync();
                        _providerAlerts.AddOrUpdate(provider.ProviderId, alerts, (_, _) => alerts);
                        return provider.ProviderId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error refreshing provider {provider.ProviderId}: {ex.Message}");
                        return null;
                    }
                });
            
            var refreshedProviders = await Task.WhenAll(refreshTasks);
            var successfulProviders = refreshedProviders.Where(p => p != null).ToList();
            
            if (successfulProviders.Any())
            {
                await NotifyGlobalAlertsChanged("bulk-refresh");
            }
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    public async Task<AlertStatistics> GetAlertStatisticsAsync()
    {
        var allAlerts = await GetAllActiveAlertsAsync();
        
        var stats = new AlertStatistics
        {
            TotalAlerts = allAlerts.Count,
            CriticalAlerts = allAlerts.Count(a => a.Priority == AlertPriority.Critical),
            UrgentAlerts = allAlerts.Count(a => a.Priority == AlertPriority.Urgent),
            WarningAlerts = allAlerts.Count(a => a.Priority == AlertPriority.Warning),
            InfoAlerts = allAlerts.Count(a => a.Priority == AlertPriority.Info),
            AlertsByType = allAlerts.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count())
        };
        
        return stats;
    }

    public IReadOnlyList<IGlobalAlertProvider> GetRegisteredProviders()
    {
        return _providers.Values.ToList().AsReadOnly();
    }

    private async void OnProviderAlertsChanged(object? sender, AlertsChangedEventArgs e)
    {
        if (sender is IGlobalAlertProvider provider)
        {
            // Update cached alerts for this provider
            var alerts = await provider.GetActiveAlertsAsync();
            _providerAlerts.AddOrUpdate(provider.ProviderId, alerts, (_, _) => alerts);
            
            await NotifyGlobalAlertsChanged(provider.ProviderId);
        }
    }

    private async Task NotifyGlobalAlertsChanged(string changedProviderId)
    {
        try
        {
            var allAlerts = await GetAllActiveAlertsAsync();
            var stats = await GetAlertStatisticsAsync();
            
            var eventArgs = new GlobalAlertsChangedEventArgs
            {
                AllAlerts = allAlerts,
                TotalCount = stats.TotalAlerts,
                CriticalCount = stats.CriticalAlerts,
                UrgentCount = stats.UrgentAlerts,
                WarningCount = stats.WarningAlerts,
                ChangedProviderId = changedProviderId
            };
            
            GlobalAlertsChanged?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying global alerts changed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _refreshTimer?.Dispose();
        
        foreach (var provider in _providers.Values)
        {
            try
            {
                provider.AlertsChanged -= OnProviderAlertsChanged;
                provider.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing provider: {ex.Message}");
            }
        }
        
        _providers.Clear();
        _providerAlerts.Clear();
        _refreshSemaphore?.Dispose();
    }
}