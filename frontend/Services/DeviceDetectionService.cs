using Microsoft.JSInterop;

namespace frontend.Services;

public interface IDeviceDetectionService
{
    Task<bool> IsMobileAsync();
    Task<DeviceInfo> GetDeviceInfoAsync();
    Task<IJSObjectReference> StartScreenSizeMonitoringAsync(Func<DeviceInfo, Task> onScreenSizeChanged);
    Task StopScreenSizeMonitoringAsync(IJSObjectReference cleanupFunction);
}

public class DeviceDetectionService : IDeviceDetectionService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IServiceProvider? _serviceProvider;
    private IJSObjectReference? _module;
    private readonly List<IJSObjectReference> _activeListeners = new();

    public DeviceDetectionService(IJSRuntime jsRuntime, IServiceProvider? serviceProvider = null)
    {
        _jsRuntime = jsRuntime;
        _serviceProvider = serviceProvider;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/deviceDetection.js");
        }
        return _module;
    }

    public async Task<bool> IsMobileAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("deviceDetection.isMobile");
        }
        catch (Exception)
        {
            // Fallback to false (desktop) if JavaScript fails
            return false;
        }
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        try
        {
            // Try to get cached device info first
            var cacheService = (IBrowserCacheService?)_serviceProvider?.GetService(typeof(IBrowserCacheService));
            if (cacheService != null)
            {
                var cachedDeviceInfo = await cacheService.GetCachedDataAsync<DeviceInfo>("device_info");
                if (cachedDeviceInfo != null)
                {
                    return cachedDeviceInfo;
                }
            }
            
            var module = await GetModuleAsync();
            var deviceInfo = await module.InvokeAsync<DeviceInfo>("deviceDetection.getDeviceInfo");
            
            // Cache the device info with longer expiry
            if (cacheService != null)
            {
                await cacheService.SetCachedDataAsync("device_info", deviceInfo, TimeSpan.FromMinutes(30));
            }
            
            return deviceInfo;
        }
        catch (Exception)
        {
            // Fallback device info
            return new DeviceInfo
            {
                IsMobile = false,
                ScreenWidth = 1920,
                ScreenHeight = 1080,
                UserAgent = "Unknown",
                IsTouchDevice = false,
                Platform = "Unknown",
                DevicePixelRatio = 1.0f
            };
        }
    }

    public async Task<IJSObjectReference> StartScreenSizeMonitoringAsync(Func<DeviceInfo, Task> onScreenSizeChanged)
    {
        try
        {
            var module = await GetModuleAsync();
            var dotNetRef = DotNetObjectReference.Create(new ScreenSizeChangeHandler(onScreenSizeChanged));
            
            var cleanupFunction = await module.InvokeAsync<IJSObjectReference>(
                "deviceDetection.onScreenSizeChange",
                DotNetObjectReference.Create(new ScreenSizeCallbackWrapper(onScreenSizeChanged))
            );
            
            _activeListeners.Add(cleanupFunction);
            return cleanupFunction;
        }
        catch (Exception)
        {
            // Return a dummy object reference if JavaScript fails
            return await _jsRuntime.InvokeAsync<IJSObjectReference>("eval", "(() => () => {})");
        }
    }

    public async Task StopScreenSizeMonitoringAsync(IJSObjectReference cleanupFunction)
    {
        try
        {
            if (cleanupFunction != null)
            {
                await cleanupFunction.InvokeVoidAsync("call", (object?)null);
                _activeListeners.Remove(cleanupFunction);
                await cleanupFunction.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up all active listeners
        foreach (var listener in _activeListeners.ToList())
        {
            await StopScreenSizeMonitoringAsync(listener);
        }
        _activeListeners.Clear();

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}

public class DeviceInfo
{
    public bool IsMobile { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public bool IsTouchDevice { get; set; }
    public string Platform { get; set; } = string.Empty;
    public float DevicePixelRatio { get; set; }
}

public class ScreenSizeChangeHandler
{
    private readonly Func<DeviceInfo, Task> _callback;

    public ScreenSizeChangeHandler(Func<DeviceInfo, Task> callback)
    {
        _callback = callback;
    }

    [JSInvokable]
    public async Task OnScreenSizeChanged(DeviceInfo deviceInfo)
    {
        await _callback(deviceInfo);
    }
}

public class ScreenSizeCallbackWrapper
{
    private readonly Func<DeviceInfo, Task> _callback;

    public ScreenSizeCallbackWrapper(Func<DeviceInfo, Task> callback)
    {
        _callback = callback;
    }

    [JSInvokable]
    public async Task Invoke(DeviceInfo deviceInfo)
    {
        await _callback(deviceInfo);
    }
}