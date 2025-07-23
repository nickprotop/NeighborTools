using Microsoft.JSInterop;
using MudBlazor;

namespace frontend.Services;

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = false;
    private bool _isInitialized = false;

    public bool IsDarkMode => _isDarkMode;

    public event EventHandler<bool>? ThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }
        
        try
        {
            // Check localStorage for saved preference
            var savedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "theme-preference");
            
            if (!string.IsNullOrEmpty(savedTheme))
            {
                _isDarkMode = savedTheme == "dark";
            }
            else
            {
                // Check system preference if no saved preference
                try
                {
                    var mediaQuery = await _jsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
                    _isDarkMode = mediaQuery;
                }
                catch
                {
                    // Default to light theme if system preference detection fails
                    _isDarkMode = false;
                }
                
                // Save the detected preference
                await SaveThemePreferenceAsync(_isDarkMode);
            }
        }
        catch
        {
            // Default to light theme if detection fails
            _isDarkMode = false;
        }

        _isInitialized = true;
        
        // Update CSS variables on initialization
        await UpdateCssVariablesAsync(_isDarkMode);
    }

    public async Task<bool> GetThemePreferenceAsync()
    {
        if (!_isInitialized)
            await InitializeAsync();
            
        return _isDarkMode;
    }

    public async Task SetThemePreferenceAsync(bool isDarkMode)
    {
        if (_isDarkMode == isDarkMode)
        {
            return;
        }

        _isDarkMode = isDarkMode;
        await SaveThemePreferenceAsync(isDarkMode);
        await UpdateCssVariablesAsync(isDarkMode);
        
        ThemeChanged?.Invoke(this, isDarkMode);
    }

    private async Task SaveThemePreferenceAsync(bool isDarkMode)
    {
        try
        {
            var themeValue = isDarkMode ? "dark" : "light";
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme-preference", themeValue);
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    private async Task UpdateCssVariablesAsync(bool isDarkMode)
    {
        try
        {
            var theme = GetAppTheme();
            var cssUpdates = new Dictionary<string, string>();
            
            if (isDarkMode)
            {
                var palette = theme.PaletteDark;
                cssUpdates = new Dictionary<string, string>
                {
                    // Core palette colors
                    { "--mud-palette-primary", palette.Primary.ToString() },
                    { "--mud-palette-secondary", palette.Secondary.ToString() },
                    { "--mud-palette-surface", palette.Surface.ToString() },
                    { "--mud-palette-background", palette.Background.ToString() },
                    { "--mud-palette-divider", palette.Divider.ToString() },
                    
                    // Text colors
                    { "--mud-text-primary", palette.TextPrimary.ToString() },
                    { "--mud-text-secondary", palette.TextSecondary.ToString() },
                    
                    // Action colors
                    { "--mud-palette-action-default", palette.ActionDefault.ToString() },
                    { "--mud-palette-action-disabled", palette.ActionDisabled.ToString() },
                    
                    // Interactive colors with proper transparency
                    { "--mud-palette-action-hover", "rgba(255, 255, 255, 0.08)" },
                    { "--mud-palette-primary-lighten", "rgba(139, 92, 246, 0.15)" },
                    { "--mud-palette-primary-darken", "#7C3AED" },
                    
                    // Status colors
                    { "--mud-palette-success", palette.Success.ToString() },
                    { "--mud-palette-warning", palette.Warning.ToString() },
                    { "--mud-palette-error", palette.Error.ToString() },
                    { "--mud-palette-info", palette.Info.ToString() },
                    
                    // Border colors
                    { "--mud-border-primary", palette.Divider.ToString() },
                    { "--mud-border-secondary", palette.DividerLight.ToString() }
                };
            }
            else
            {
                var palette = theme.PaletteLight;
                cssUpdates = new Dictionary<string, string>
                {
                    // Core palette colors
                    { "--mud-palette-primary", palette.Primary.ToString() },
                    { "--mud-palette-secondary", palette.Secondary.ToString() },
                    { "--mud-palette-surface", palette.Surface.ToString() },
                    { "--mud-palette-background", palette.Background.ToString() },
                    { "--mud-palette-divider", palette.Divider.ToString() },
                    
                    // Text colors
                    { "--mud-text-primary", palette.TextPrimary.ToString() },
                    { "--mud-text-secondary", palette.TextSecondary.ToString() },
                    
                    // Action colors
                    { "--mud-palette-action-default", palette.ActionDefault.ToString() },
                    { "--mud-palette-action-disabled", palette.ActionDisabled.ToString() },
                    
                    // Interactive colors with proper transparency
                    { "--mud-palette-action-hover", "rgba(0, 0, 0, 0.04)" },
                    { "--mud-palette-primary-lighten", "rgba(99, 102, 241, 0.15)" },
                    { "--mud-palette-primary-darken", "#3730A3" },
                    
                    // Status colors
                    { "--mud-palette-success", palette.Success.ToString() },
                    { "--mud-palette-warning", palette.Warning.ToString() },
                    { "--mud-palette-error", palette.Error.ToString() },
                    { "--mud-palette-info", palette.Info.ToString() },
                    
                    // Border colors
                    { "--mud-border-primary", palette.Divider.ToString() },
                    { "--mud-border-secondary", palette.DividerLight.ToString() }
                };
            }

            foreach (var update in cssUpdates)
            {
                await _jsRuntime.InvokeVoidAsync("eval", 
                    $"document.documentElement.style.setProperty('{update.Key}', '{update.Value}')");
            }
        }
        catch
        {
            // Ignore CSS update errors
        }
    }

    public MudTheme GetLightTheme()
    {
        return GetAppTheme();
    }

    public MudTheme GetDarkTheme()
    {
        return GetAppTheme();
    }

    public MudTheme GetCurrentTheme()
    {
        return GetAppTheme();
    }

    private MudTheme GetAppTheme()
    {
        return new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#6366F1", // Modern purple from CSS
                Secondary = "#06B6D4", // Modern teal from CSS
                AppbarBackground = "#FFFFFF",
                AppbarText = "#111827",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#111827",
                DrawerIcon = "#6B7280",
                Surface = "#FFFFFF",
                Background = "#F9FAFB", // Light background
                BackgroundGray = "#F3F4F6",
                TextPrimary = "#111827",
                TextSecondary = "#6B7280",
                ActionDefault = "#6B7280",
                ActionDisabled = "#9CA3AF",
                ActionDisabledBackground = "#F3F4F6",
                Divider = "#E5E7EB",
                DividerLight = "#F3F4F6",
                TableLines = "#E5E7EB",
                LinesDefault = "#E5E7EB",
                LinesInputs = "#D1D5DB",
                TextDisabled = "#9CA3AF",
                Info = "#3B82F6",
                Success = "#22C55E",
                Warning = "#F59E0B",
                Error = "#EF4444",
                Dark = "#111827"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#8B5CF6", // Lighter purple for dark mode
                Secondary = "#22D3EE", // Lighter teal for dark mode
                AppbarBackground = "#111827",
                AppbarText = "#F9FAFB",
                DrawerBackground = "#111827",
                DrawerText = "#F9FAFB",
                DrawerIcon = "#9CA3AF",
                Surface = "#1F2937", // Dark surface
                Background = "#111827", // Dark background
                BackgroundGray = "#0F172A",
                TextPrimary = "#F9FAFB",
                TextSecondary = "#D1D5DB",
                ActionDefault = "#9CA3AF",
                ActionDisabled = "#6B7280",
                ActionDisabledBackground = "#374151",
                Divider = "#374151",
                DividerLight = "#1F2937",
                TableLines = "#374151",
                LinesDefault = "#374151",
                LinesInputs = "#4B5563",
                TextDisabled = "#6B7280",
                Info = "#60A5FA",
                Success = "#34D399",
                Warning = "#FBBF24",
                Error = "#F87171",
                Dark = "#F9FAFB"
            }
        };
    }
}