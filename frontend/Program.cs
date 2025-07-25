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
builder.Services.AddScoped<ISessionTimeoutService, SessionTimeoutService>();
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

var app = builder.Build();

// Restore authentication state on app startup
var authService = app.Services.GetRequiredService<IAuthService>();
await authService.RestoreAuthenticationAsync();

await app.RunAsync();
