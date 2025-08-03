using frontend.Models;
using frontend.Models.Alerts;
using frontend.Services.Alerts;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using MudBlazor;

namespace frontend.Services.Alerts.Providers;

/// <summary>
/// Alert provider for payment setup notifications
/// </summary>
public class PaymentSetupAlertProvider : IGlobalAlertProvider
{
    private readonly IToolService _toolService;
    private readonly IPaymentService _paymentService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly Timer _refreshTimer;
    private bool _disposed = false;
    private bool _isDismissed = false;
    private string? _currentUserId;

    public string ProviderId => "payment-setup";
    public AlertType AlertType => AlertType.PaymentSetup;
    public bool IsEnabled { get; private set; } = true;
    
    public AlertProviderConfig Configuration { get; } = new()
    {
        IsEnabled = true,
        RefreshInterval = TimeSpan.FromMinutes(10), // Check less frequently for payment setup
        MaxAlertsToShow = 1,
        MinimumPriority = AlertPriority.Warning,
        ShowInGlobalMode = true
    };

    public event EventHandler<AlertsChangedEventArgs>? AlertsChanged;

    public PaymentSetupAlertProvider(
        IToolService toolService,
        IPaymentService paymentService,
        AuthenticationStateProvider authStateProvider)
    {
        _toolService = toolService;
        _paymentService = paymentService;
        _authStateProvider = authStateProvider;
        
        // Refresh every 10 minutes for payment setup checks
        _refreshTimer = new Timer(async _ => await RefreshAlertsAsync(), 
                                 null, TimeSpan.Zero, Configuration.RefreshInterval);
    }

    public async Task InitializeAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(_currentUserId) || !authState.User.Identity?.IsAuthenticated == true)
        {
            IsEnabled = false;
            return;
        }

        await RefreshAlertsAsync();
    }

    public async Task<List<AlertItem>> GetActiveAlertsAsync()
    {
        if (!IsEnabled || _isDismissed || string.IsNullOrEmpty(_currentUserId))
        {
            return new List<AlertItem>();
        }

        try
        {
            // Check if user has tools
            var toolsResult = await _toolService.GetMyToolsAsync();
            if (!toolsResult.Success || toolsResult.Data == null || !toolsResult.Data.Any())
            {
                return new List<AlertItem>();
            }

            var toolCount = toolsResult.Data.Count();

            // Check if payment settings are configured
            var paymentResult = await _paymentService.GetPaymentSettingsAsync();
            
            bool needsPaymentSetup = false;
            if (paymentResult.Success && paymentResult.Data?.Settings != null)
            {
                var settings = paymentResult.Data.Settings;
                needsPaymentSetup = string.IsNullOrEmpty(settings.PayPalEmail);
            }
            else
            {
                // If we can't get settings or they don't exist, assume they need setup
                needsPaymentSetup = true;
            }

            if (!needsPaymentSetup)
            {
                return new List<AlertItem>();
            }

            // Create payment setup alert
            var alert = new AlertItem
            {
                Id = "payment-setup-required",
                Type = AlertType.PaymentSetup,
                Priority = AlertPriority.Warning,
                Severity = GlobalAlertSeverity.Warning,
                Title = "Payment Setup Required",
                Description = $"You have {toolCount} tool(s) listed for rent, but haven't configured your PayPal email for receiving payments. Without payment settings, you won't receive payouts when someone rents your tools.",
                Icon = Icons.Material.Filled.Payment,
                IsDismissible = true,
                ShowProgress = false,
                CustomClass = "priority-warning",
                Actions = new List<AlertAction>
                {
                    new AlertAction
                    {
                        Id = "setup-payments",
                        Label = "Setup Payments",
                        Icon = Icons.Material.Filled.Payment,
                        Type = AlertActionType.Navigate,
                        NavigationUrl = "/settings/payments",
                        IsPrimary = true,
                        IsFullWidth = true
                    }
                },
                Data = new Dictionary<string, object>
                {
                    ["toolCount"] = toolCount,
                    ["userId"] = _currentUserId
                }
            };

            return new List<AlertItem> { alert };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting payment setup alerts: {ex.Message}");
            return new List<AlertItem>();
        }
    }

    public async Task<bool> DismissAlertAsync(string alertId)
    {
        if (alertId == "payment-setup-required")
        {
            _isDismissed = true;
            
            // Notify that alerts changed
            AlertsChanged?.Invoke(this, new AlertsChangedEventArgs
            {
                UpdatedAlerts = new List<AlertItem>(),
                RemovedAlertIds = new List<string> { alertId },
                ProviderId = ProviderId
            });
            
            return true;
        }
        
        return false;
    }

    public async Task<AlertActionResult> ExecuteActionAsync(string alertId, string actionId)
    {
        if (alertId == "payment-setup-required" && actionId == "setup-payments")
        {
            return new AlertActionResult
            {
                Success = true,
                Message = "Redirecting to payment settings...",
                ShouldRefresh = false,
                ShouldDismiss = false, // Let user dismiss manually after setup
                Data = new Dictionary<string, object>
                {
                    ["navigationUrl"] = "/settings/payments"
                }
            };
        }
        
        return new AlertActionResult
        {
            Success = false,
            Message = "Unknown action"
        };
    }

    public async Task RefreshAlertsAsync()
    {
        if (!IsEnabled) return;
        
        try
        {
            var alerts = await GetActiveAlertsAsync();
            
            AlertsChanged?.Invoke(this, new AlertsChangedEventArgs
            {
                UpdatedAlerts = alerts,
                RemovedAlertIds = new List<string>(),
                ProviderId = ProviderId
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing payment setup alerts: {ex.Message}");
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        IsEnabled = enabled;
        
        if (!enabled)
        {
            // Clear all alerts when disabled
            AlertsChanged?.Invoke(this, new AlertsChangedEventArgs
            {
                UpdatedAlerts = new List<AlertItem>(),
                RemovedAlertIds = new List<string> { "payment-setup-required" },
                ProviderId = ProviderId
            });
        }
        else
        {
            await RefreshAlertsAsync();
        }
    }

    public void RefreshStatus()
    {
        // Reset dismissal state to allow re-checking
        _isDismissed = false;
        _ = Task.Run(async () => await RefreshAlertsAsync());
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _refreshTimer?.Dispose();
    }
}