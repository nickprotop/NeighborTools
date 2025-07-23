using MudBlazor;

namespace frontend.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    Task<bool> GetThemePreferenceAsync();
    Task SetThemePreferenceAsync(bool isDarkMode);
    event EventHandler<bool>? ThemeChanged;
    Task InitializeAsync();
    MudTheme GetLightTheme();
    MudTheme GetDarkTheme();
    MudTheme GetCurrentTheme();
}