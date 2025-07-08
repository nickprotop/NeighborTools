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
builder.Services.AddScoped(sp => 
{
    // PRODUCTION WARNING: Change this to the actual API server location in production
    // Use the same host as the frontend, but point to API port
    var apiBaseUrl = builder.HostEnvironment.BaseAddress.Replace(":5000", ":5002").Replace(":5001", ":5002");
    
    // NOTE: In Blazor WebAssembly, SSL certificate validation is handled by the browser
    // Self-signed certificates will show a browser warning that users must accept
    // For development, you can:
    // 1. Accept the browser certificate warning when first accessing the API
    // 2. Use HTTP instead of HTTPS for local development
    // 3. Install the self-signed certificate in the browser's trust store
    
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    return httpClient;
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<IRentalService, RentalService>();

await builder.Build().RunAsync();
