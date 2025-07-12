using Microsoft.JSInterop;
using frontend.Models;

namespace frontend.Services;

public interface ISessionTimeoutService
{
    Task InitializeAsync();
    Task UpdateTimeoutAsync(int timeoutMinutes);
    Task ResetActivityAsync();
    event Action? OnSessionExpiring;
    event Action? OnSessionExpired;
    void Dispose();
}

public class SessionTimeoutService : ISessionTimeoutService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAuthService _authService;
    private readonly ILogger<SessionTimeoutService> _logger;
    private Timer? _sessionTimer;
    private Timer? _warningTimer;
    private DateTime _lastActivity = DateTime.UtcNow;
    private int _timeoutMinutes = 480; // Default 8 hours
    private bool _disposed = false;

    public event Action? OnSessionExpiring;
    public event Action? OnSessionExpired;

    public SessionTimeoutService(
        IJSRuntime jsRuntime,
        IAuthService authService,
        ILogger<SessionTimeoutService> logger)
    {
        _jsRuntime = jsRuntime;
        _authService = authService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Set up JavaScript event listeners for user activity
            await _jsRuntime.InvokeVoidAsync("sessionTimeout.initialize", 
                DotNetObjectReference.Create(this));
            
            _lastActivity = DateTime.UtcNow;
            StartTimers();
            
            _logger.LogInformation("Session timeout service initialized with {TimeoutMinutes} minutes", _timeoutMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize session timeout service");
        }
    }

    public async Task UpdateTimeoutAsync(int timeoutMinutes)
    {
        _timeoutMinutes = timeoutMinutes;
        _lastActivity = DateTime.UtcNow;
        
        StopTimers();
        StartTimers();
        
        _logger.LogInformation("Session timeout updated to {TimeoutMinutes} minutes", timeoutMinutes);
        
        // Update JavaScript timeout as well
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionTimeout.updateTimeout", timeoutMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update JavaScript timeout");
        }
    }

    [JSInvokable]
    public async Task ResetActivityAsync()
    {
        _lastActivity = DateTime.UtcNow;
        
        // Reset timers
        StopTimers();
        StartTimers();
        
        _logger.LogDebug("User activity detected, session timer reset");
    }

    private void StartTimers()
    {
        if (_disposed) return;

        // Main session timer
        var timeoutMs = TimeSpan.FromMinutes(_timeoutMinutes).TotalMilliseconds;
        _sessionTimer = new Timer(OnSessionTimeout, null, (int)timeoutMs, Timeout.Infinite);

        // Warning timer (5 minutes before expiry, or 1/10th of timeout if less than 50 minutes)
        var warningMinutes = _timeoutMinutes > 50 ? 5 : Math.Max(1, _timeoutMinutes / 10);
        var warningMs = TimeSpan.FromMinutes(_timeoutMinutes - warningMinutes).TotalMilliseconds;
        _warningTimer = new Timer(OnSessionWarning, null, (int)warningMs, Timeout.Infinite);
    }

    private void StopTimers()
    {
        _sessionTimer?.Dispose();
        _sessionTimer = null;
        
        _warningTimer?.Dispose();
        _warningTimer = null;
    }

    private async void OnSessionWarning(object? state)
    {
        try
        {
            _logger.LogInformation("Session expiring warning triggered");
            OnSessionExpiring?.Invoke();
            
            // Show warning in JavaScript as well
            await _jsRuntime.InvokeVoidAsync("sessionTimeout.showExpiringWarning");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in session warning handler");
        }
    }

    private async void OnSessionTimeout(object? state)
    {
        try
        {
            _logger.LogInformation("Session timeout triggered, logging out user");
            OnSessionExpired?.Invoke();
            
            // Perform logout
            await _authService.LogoutAsync();
            
            // Notify JavaScript
            await _jsRuntime.InvokeVoidAsync("sessionTimeout.onSessionExpired");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in session timeout handler");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        StopTimers();
        
        try
        {
            _jsRuntime.InvokeVoidAsync("sessionTimeout.cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup JavaScript session timeout");
        }
    }
}