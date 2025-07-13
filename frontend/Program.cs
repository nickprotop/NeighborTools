using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using frontend;
using frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API communication
builder.Services.AddScoped<AuthenticatedHttpClientHandler>();
builder.Services.AddHttpClient("api", client =>
{
    // PRODUCTION WARNING: Change this to the actual API server location in production
    // Use the same host as the frontend, but point to API port
    var apiBaseUrl = builder.HostEnvironment.BaseAddress.Replace(":5000", ":5002").Replace(":5001", ":5002");
    client.BaseAddress = new Uri(apiBaseUrl);
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

var app = builder.Build();

// Restore authentication state on app startup
var authService = app.Services.GetRequiredService<IAuthService>();
await authService.RestoreAuthenticationAsync();

await app.RunAsync();
