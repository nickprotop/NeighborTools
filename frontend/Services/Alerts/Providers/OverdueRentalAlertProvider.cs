using frontend.Models;
using frontend.Models.Alerts;
using frontend.Services.Alerts;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using MudBlazor;

namespace frontend.Services.Alerts.Providers;

/// <summary>
/// Alert provider for overdue rental notifications
/// </summary>
public class OverdueRentalAlertProvider : IGlobalAlertProvider
{
    private readonly IRentalService _rentalService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly Timer _refreshTimer;
    private readonly HashSet<string> _dismissedAlerts = new();
    private bool _disposed = false;
    private string? _currentUserId;

    public string ProviderId => "overdue-rentals";
    public AlertType AlertType => AlertType.OverdueRental;
    public bool IsEnabled { get; private set; } = true;
    
    public AlertProviderConfig Configuration { get; } = new()
    {
        IsEnabled = true,
        RefreshInterval = TimeSpan.FromMinutes(5), // Frequent refresh for overdue rentals
        MaxAlertsToShow = 10,
        MinimumPriority = AlertPriority.Info,
        ShowInGlobalMode = true
    };

    public event EventHandler<AlertsChangedEventArgs>? AlertsChanged;

    public OverdueRentalAlertProvider(
        IRentalService rentalService,
        AuthenticationStateProvider authStateProvider)
    {
        _rentalService = rentalService;
        _authStateProvider = authStateProvider;
        
        // Refresh every 5 minutes for overdue rental checks
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
        if (!IsEnabled || string.IsNullOrEmpty(_currentUserId))
        {
            return new List<AlertItem>();
        }

        try
        {
            var allOverdueRentals = new List<Rental>();
            
            // Get rentals where user is renter
            var renterResult = await _rentalService.GetMyRentalsAsync("Overdue");
            if (renterResult.Success && renterResult.Data != null)
            {
                var renterOverdueRentals = renterResult.Data
                    .Where(r => (r.Status == "PickedUp" || r.Status == "Overdue") && 
                               r.EndDate < DateTime.UtcNow &&
                               !_dismissedAlerts.Contains(r.Id))
                    .ToList();
                
                allOverdueRentals.AddRange(renterOverdueRentals);
            }
            
            // Get rentals where user is owner
            var ownerResult = await _rentalService.GetMyToolRentalsAsync("Overdue");
            if (ownerResult.Success && ownerResult.Data != null)
            {
                var ownerOverdueRentals = ownerResult.Data
                    .Where(r => (r.Status == "PickedUp" || r.Status == "Overdue") && 
                               r.EndDate < DateTime.UtcNow &&
                               !_dismissedAlerts.Contains(r.Id))
                    .ToList();
                
                allOverdueRentals.AddRange(ownerOverdueRentals);
            }
            
            // Remove duplicates by rental ID
            var overdueRentals = allOverdueRentals
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .ToList();

            var alerts = overdueRentals.Select(CreateAlertFromRental).ToList();
            return alerts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting overdue rental alerts: {ex.Message}");
            return new List<AlertItem>();
        }
    }

    private AlertItem CreateAlertFromRental(Rental rental)
    {
        var daysOverdue = GetDaysOverdue(rental);
        var isOwner = IsUserOwner(rental);
        
        var alert = new AlertItem
        {
            Id = $"overdue-rental-{rental.Id}",
            Type = AlertType.OverdueRental,
            Priority = GetAlertPriority(daysOverdue),
            Severity = GetAlertSeverity(daysOverdue),
            Title = GetAlertTitle(rental, daysOverdue, isOwner),
            Description = GetAlertDescription(rental, daysOverdue, isOwner),
            Icon = GetAlertIcon(daysOverdue),
            IsDismissible = true,
            ShowProgress = true,
            ProgressValue = GetOverdueProgress(daysOverdue),
            ProgressColor = GetOverdueProgressColor(daysOverdue),
            CustomClass = $"priority-{GetAlertPriority(daysOverdue).ToString().ToLower()} {GetAlertClass(daysOverdue)}",
            Actions = CreateActionsForRental(rental, isOwner),
            Data = new Dictionary<string, object>
            {
                ["rentalId"] = rental.Id,
                ["toolName"] = rental.ToolName,
                ["daysOverdue"] = daysOverdue,
                ["isOwner"] = isOwner,
                ["endDate"] = rental.EndDate
            }
        };

        return alert;
    }

    private List<AlertAction> CreateActionsForRental(Rental rental, bool isOwner)
    {
        var actions = new List<AlertAction>();

        // View Details action
        actions.Add(new AlertAction
        {
            Id = "view-details",
            Label = "View Details",
            Icon = Icons.Material.Filled.Assignment,
            Type = AlertActionType.Navigate,
            NavigationUrl = $"/rentals/{rental.Id}",
            IsFullWidth = true
        });

        // Mark as Returned action (for both renter and owner)
        if (IsUserRenter(rental) || isOwner)
        {
            actions.Add(new AlertAction
            {
                Id = "mark-returned",
                Label = isOwner ? "Confirm Return" : "Mark as Returned",
                Icon = Icons.Material.Filled.CheckCircle,
                Type = AlertActionType.Execute,
                IsFullWidth = true
            });
        }

        // Contact action (owner contacting renter)
        if (isOwner)
        {
            actions.Add(new AlertAction
            {
                Id = "contact-renter",
                Label = "Contact Renter",
                Icon = Icons.Material.Filled.Phone,
                Type = AlertActionType.Execute,
                IsFullWidth = true
            });
        }

        // Create Dispute action
        if (ShouldShowDisputeButton(rental))
        {
            actions.Add(new AlertAction
            {
                Id = "create-dispute",
                Label = "Create Dispute",
                Icon = Icons.Material.Filled.Report,
                Type = AlertActionType.Execute,
                IsFullWidth = true
            });
        }

        return actions;
    }

    public async Task<bool> DismissAlertAsync(string alertId)
    {
        if (alertId.StartsWith("overdue-rental-"))
        {
            var rentalId = alertId.Replace("overdue-rental-", "");
            _dismissedAlerts.Add(rentalId);
            
            // Notify that alerts changed
            AlertsChanged?.Invoke(this, new AlertsChangedEventArgs
            {
                UpdatedAlerts = await GetActiveAlertsAsync(),
                RemovedAlertIds = new List<string> { alertId },
                ProviderId = ProviderId
            });
            
            return true;
        }
        
        return false;
    }

    public async Task<AlertActionResult> ExecuteActionAsync(string alertId, string actionId)
    {
        if (!alertId.StartsWith("overdue-rental-"))
        {
            return new AlertActionResult { Success = false, Message = "Invalid alert ID" };
        }

        var rentalId = alertId.Replace("overdue-rental-", "");

        return actionId switch
        {
            "view-details" => new AlertActionResult
            {
                Success = true,
                Message = "Redirecting to rental details...",
                Data = new Dictionary<string, object> { ["navigationUrl"] = $"/rentals/{rentalId}" }
            },
            "mark-returned" => await HandleMarkAsReturned(rentalId),
            "contact-renter" => await HandleContactRenter(rentalId),
            "create-dispute" => await HandleCreateDispute(rentalId),
            _ => new AlertActionResult { Success = false, Message = "Unknown action" }
        };
    }

    private async Task<AlertActionResult> HandleMarkAsReturned(string rentalId)
    {
        // This would typically open a dialog or navigate to a return confirmation page
        return new AlertActionResult
        {
            Success = true,
            Message = "Opening return confirmation...",
            ShouldRefresh = true,
            Data = new Dictionary<string, object>
            {
                ["action"] = "open-return-dialog",
                ["rentalId"] = rentalId
            }
        };
    }

    private async Task<AlertActionResult> HandleContactRenter(string rentalId)
    {
        return new AlertActionResult
        {
            Success = true,
            Message = "Opening contact options...",
            Data = new Dictionary<string, object>
            {
                ["action"] = "contact-renter",
                ["rentalId"] = rentalId
            }
        };
    }

    private async Task<AlertActionResult> HandleCreateDispute(string rentalId)
    {
        return new AlertActionResult
        {
            Success = true,
            Message = "Opening dispute creation...",
            Data = new Dictionary<string, object>
            {
                ["action"] = "create-dispute",
                ["rentalId"] = rentalId
            }
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
            Console.WriteLine($"Error refreshing overdue rental alerts: {ex.Message}");
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
                RemovedAlertIds = new List<string>(),
                ProviderId = ProviderId
            });
        }
        else
        {
            await RefreshAlertsAsync();
        }
    }

    // Helper methods from original component
    private int GetDaysOverdue(Rental rental)
    {
        return (int)(DateTime.UtcNow - rental.EndDate).TotalDays;
    }

    private bool IsUserRenter(Rental rental)
    {
        return rental.RenterId == _currentUserId;
    }

    private bool IsUserOwner(Rental rental)
    {
        return rental.OwnerId == _currentUserId;
    }

    private AlertPriority GetAlertPriority(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 14 => AlertPriority.Critical,
            >= 7 => AlertPriority.Urgent,
            >= 3 => AlertPriority.Warning,
            _ => AlertPriority.Info
        };
    }

    private GlobalAlertSeverity GetAlertSeverity(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 7 => GlobalAlertSeverity.Error,
            >= 3 => GlobalAlertSeverity.Warning,
            >= 1 => GlobalAlertSeverity.Warning,
            _ => GlobalAlertSeverity.Info
        };
    }

    private string GetAlertClass(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 14 => "critical",
            >= 7 => "severe",
            >= 3 => "moderate",
            _ => "recent"
        };
    }

    private string GetAlertIcon(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 7 => Icons.Material.Filled.Error,
            >= 3 => Icons.Material.Filled.Warning,
            _ => Icons.Material.Filled.Schedule
        };
    }

    private string GetAlertTitle(Rental rental, int daysOverdue, bool isOwner)
    {
        return daysOverdue switch
        {
            >= 14 => isOwner ? "Critical: Tool Severely Overdue" : "Critical: You Must Return This Tool",
            >= 7 => isOwner ? "Urgent: Tool Overdue for 1+ Week" : "Urgent: Tool Return Overdue",
            >= 3 => isOwner ? "Tool Overdue for 3+ Days" : "Tool Return Overdue",
            _ => isOwner ? "Renter Has Overdue Tool" : "Tool Return is Overdue"
        };
    }

    private string GetAlertDescription(Rental rental, int daysOverdue, bool isOwner)
    {
        if (isOwner)
        {
            return $"{rental.RenterName} has had your {rental.ToolName} for {daysOverdue} day{(daysOverdue > 1 ? "s" : "")} past the return date.";
        }
        else
        {
            return $"You need to return {rental.ToolName} to {rental.OwnerName}. It was due {daysOverdue} day{(daysOverdue > 1 ? "s" : "")} ago.";
        }
    }

    private double GetOverdueProgress(int daysOverdue)
    {
        return Math.Min(100, (daysOverdue / 14.0) * 100); // Max progress at 14 days
    }

    private string GetOverdueProgressColor(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 14 => "error",
            >= 7 => "warning",
            >= 3 => "warning",
            _ => "info"
        };
    }

    private bool ShouldShowDisputeButton(Rental rental)
    {
        // Show dispute button if:
        // 1. Rental is overdue or returned
        // 2. User is involved in the rental (renter or owner)
        // 3. Within dispute window (if returned) or anytime (if overdue)
        
        if (!IsUserRenter(rental) && !IsUserOwner(rental))
            return false;
            
        // For overdue rentals, always show dispute button
        if (rental.Status == "Overdue" || (rental.Status == "PickedUp" && rental.EndDate < DateTime.UtcNow))
            return true;
            
        // For returned rentals, show if within dispute window
        if (rental.Status == "Returned" && rental.DisputeDeadline.HasValue)
            return DateTime.UtcNow <= rental.DisputeDeadline.Value;
            
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _refreshTimer?.Dispose();
    }
}