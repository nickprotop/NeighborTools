using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Web;

namespace frontend.Services;

public interface INavigationHelperService
{
    Task NavigateBack(string fallbackUrl = "/");
    string GetReturnUrl(string fallbackUrl = "/");
    void SetReturnUrl(string url);
    void NavigateToRoute(string route);
    void NavigateToRoute(string route, bool forceLoad);
}

public class NavigationHelperService : INavigationHelperService
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private string? _storedReturnUrl;

    public NavigationHelperService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public async Task NavigateBack(string fallbackUrl = "/")
    {
        var returnUrl = GetReturnUrl(fallbackUrl);
        
        // Try to use browser's back button if we can detect it's safe
        if (await CanUseHistoryBack())
        {
            await _jsRuntime.InvokeVoidAsync("history.back");
        }
        else
        {
            _navigationManager.NavigateTo(returnUrl);
        }
    }

    public string GetReturnUrl(string fallbackUrl = "/")
    {
        // Priority order:
        // 1. Stored return URL (manually set)
        // 2. 'returnUrl' query parameter
        // 3. 'from' query parameter  
        // 4. Referrer header analysis
        // 5. Fallback URL

        if (!string.IsNullOrEmpty(_storedReturnUrl))
        {
            return _storedReturnUrl;
        }

        var uri = new Uri(_navigationManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        
        // Check for returnUrl parameter (most explicit)
        var returnUrl = query["returnUrl"];
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return returnUrl;
        }

        // Check for from parameter (existing pattern)
        var from = query["from"];
        if (!string.IsNullOrEmpty(from))
        {
            return MapFromParameterToUrl(from);
        }

        // Try to analyze referrer
        var referrerUrl = AnalyzeReferrer();
        if (!string.IsNullOrEmpty(referrerUrl))
        {
            return referrerUrl;
        }

        return fallbackUrl;
    }

    public void SetReturnUrl(string url)
    {
        _storedReturnUrl = url;
    }

    public void NavigateToRoute(string route)
    {
        NavigateToRoute(route, false);
    }

    public void NavigateToRoute(string route, bool forceLoad)
    {
        // Convert absolute paths to relative paths for base href compatibility
        var relativeRoute = route.StartsWith("/") ? route.Substring(1) : route;
        
        // Handle empty route (root)
        if (string.IsNullOrEmpty(relativeRoute))
        {
            relativeRoute = "";
        }
        
        _navigationManager.NavigateTo(relativeRoute, forceLoad);
    }

    private async Task<bool> CanUseHistoryBack()
    {
        try
        {
            // Check if there's history to go back to and it's within our domain
            var hasHistory = await _jsRuntime.InvokeAsync<bool>("eval", "window.history.length > 1");
            return hasHistory;
        }
        catch
        {
            return false;
        }
    }

    private string MapFromParameterToUrl(string from)
    {
        return from.ToLower() switch
        {
            "my-tools" => "my-tools",
            "tools" => "tools",
            "home" => "",
            "dashboard" => "dashboard",
            "rentals" => "my-rentals",
            "search" => "tools",
            _ => "tools" // Default to tools page
        };
    }

    private string? AnalyzeReferrer()
    {
        // This is limited in Blazor WASM, but we can try to infer from current URL patterns
        var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;
        
        // If we're viewing a tool detail and came from somewhere, make educated guesses
        if (currentPath.StartsWith("/tools/") && currentPath != "/tools")
        {
            return "tools"; // Viewing a specific tool, likely came from tools list
        }
        
        if (currentPath.StartsWith("/edit-tool/"))
        {
            return "my-tools"; // Editing a tool, likely came from my tools
        }

        return null;
    }
}