using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using frontend;
using frontend.Services;
using ToolsSharing.Frontend.Services;
using ToolsSharing.Frontend.Configuration;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from config.json
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
AppSettings appSettings;

try
{
    var configJson = await http.GetStringAsync("config.json");
    appSettings = JsonSerializer.Deserialize<AppSettings>(configJson, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? new AppSettings();
    Console.WriteLine("✅ Using config.json");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Failed to load config.json: {ex.Message}");
    try
    {
        var configSampleJson = await http.GetStringAsync("config.sample.json");
        appSettings = JsonSerializer.Deserialize<AppSettings>(configSampleJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();
        Console.WriteLine("⚠️ Using config.sample.json - Please create config.json with your actual values");
    }
    catch (Exception fallbackEx)
    {
        Console.WriteLine($"❌ Failed to load config.sample.json: {fallbackEx.Message}");
        Console.WriteLine("⚠️ Using default configuration");
        appSettings = new AppSettings();
    }
}

// Ensure MapSettings has proper defaults if missing or incomplete
EnsureMapSettingsDefaults(appSettings);

// Register configuration in DI
builder.Services.AddSingleton(appSettings);

// Configure HttpClient for API communication
builder.Services.AddScoped<AuthenticatedHttpClientHandler>();
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(appSettings.ApiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(appSettings.ApiSettings.TimeoutSeconds);
})
.AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

// Register the named HttpClient as the default HttpClient
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<IPublicProfileService, PublicProfileService>();
builder.Services.AddScoped<INavigationHelperService, NavigationHelperService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<FavoritesService>();
builder.Services.AddScoped<SampleDataService>();
builder.Services.AddScoped<ToolsSharing.Frontend.Services.MutualClosureService>();
builder.Services.AddScoped<AdminMutualClosureService>();
builder.Services.AddScoped<BundleService>();
builder.Services.AddScoped<BundleReviewService>();
builder.Services.AddScoped<IUrlService, UrlService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IBreadcrumbService, BreadcrumbService>();
builder.Services.AddScoped<ISecurityAnalyticsService, SecurityAnalyticsService>();
builder.Services.AddScoped<ISecurityManagementService, SecurityManagementService>();
builder.Services.AddScoped<IDeviceDetectionService, DeviceDetectionService>();
builder.Services.AddScoped<IBrowserCacheService, BrowserCacheService>();

// Add location services
builder.Services.AddScoped<ToolsSharing.Frontend.Services.Location.ILocationService, ToolsSharing.Frontend.Services.Location.LocationService>();

var app = builder.Build();

// Restore authentication state on app startup
var authService = app.Services.GetRequiredService<IAuthService>();
await authService.RestoreAuthenticationAsync();

await app.RunAsync();

/// <summary>
/// Ensures MapSettings has proper defaults when config.json is missing or incomplete
/// </summary>
static void EnsureMapSettingsDefaults(AppSettings appSettings)
{
    // Initialize MapSettings if null
    appSettings.MapSettings ??= new MapSettings();
    
    var defaults = new MapSettings();
    
    // Apply defaults for missing or invalid values
    if (string.IsNullOrEmpty(appSettings.MapSettings.MapTileUrl))
    {
        appSettings.MapSettings.MapTileUrl = defaults.MapTileUrl;
        Console.WriteLine($"✅ Applied default MapTileUrl: {defaults.MapTileUrl}");
    }
    
    if (string.IsNullOrEmpty(appSettings.MapSettings.MapAttribution))
    {
        appSettings.MapSettings.MapAttribution = defaults.MapAttribution;
        Console.WriteLine($"✅ Applied default MapAttribution: {defaults.MapAttribution}");
    }
    
    if (appSettings.MapSettings.DefaultZoom < 5 || appSettings.MapSettings.DefaultZoom > 18)
    {
        appSettings.MapSettings.DefaultZoom = defaults.DefaultZoom;
        Console.WriteLine($"✅ Applied default DefaultZoom: {defaults.DefaultZoom}");
    }
    
    if (appSettings.MapSettings.MinZoom < 1 || appSettings.MapSettings.MinZoom > appSettings.MapSettings.DefaultZoom)
    {
        appSettings.MapSettings.MinZoom = defaults.MinZoom;
        Console.WriteLine($"✅ Applied default MinZoom: {defaults.MinZoom}");
    }
    
    if (appSettings.MapSettings.MaxZoom < appSettings.MapSettings.DefaultZoom || appSettings.MapSettings.MaxZoom > 20)
    {
        appSettings.MapSettings.MaxZoom = defaults.MaxZoom;
        Console.WriteLine($"✅ Applied default MaxZoom: {defaults.MaxZoom}");
    }
    
    // Initialize DefaultCenter if null
    appSettings.MapSettings.DefaultCenter ??= new MapCenter();
    
    // Apply default center coordinates if invalid (0,0 or out of valid range)
    if (appSettings.MapSettings.DefaultCenter.Lat == 0 && appSettings.MapSettings.DefaultCenter.Lng == 0 ||
        appSettings.MapSettings.DefaultCenter.Lat < -90 || appSettings.MapSettings.DefaultCenter.Lat > 90 ||
        appSettings.MapSettings.DefaultCenter.Lng < -180 || appSettings.MapSettings.DefaultCenter.Lng > 180)
    {
        appSettings.MapSettings.DefaultCenter.Lat = defaults.DefaultCenter.Lat;
        appSettings.MapSettings.DefaultCenter.Lng = defaults.DefaultCenter.Lng;
        Console.WriteLine($"✅ Applied default map center: {defaults.DefaultCenter.Lat}, {defaults.DefaultCenter.Lng}");
    }
    
    // Apply default timeout values if invalid
    if (appSettings.MapSettings.LocationTimeout <= 0)
    {
        appSettings.MapSettings.LocationTimeout = defaults.LocationTimeout;
        Console.WriteLine($"✅ Applied default LocationTimeout: {defaults.LocationTimeout}ms");
    }
    
    if (appSettings.MapSettings.MaxLocationAge <= 0)
    {
        appSettings.MapSettings.MaxLocationAge = defaults.MaxLocationAge;
        Console.WriteLine($"✅ Applied default MaxLocationAge: {defaults.MaxLocationAge}ms");
    }
    
    Console.WriteLine("🗺️ MapSettings validation completed");
}
