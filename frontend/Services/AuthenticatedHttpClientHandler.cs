using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using frontend.Models;
using Microsoft.AspNetCore.Components;

namespace frontend.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;

    public AuthenticatedHttpClientHandler(
        ILocalStorageService localStorage, 
        AuthenticationStateProvider authStateProvider,
        IServiceProvider serviceProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the token from storage
        var token = await _localStorage.GetItemAsync<string>("authToken") ?? 
                   await _localStorage.GetSessionItemAsync<string>("authToken");

        // Add Authorization header if token exists and is valid format
        if (!string.IsNullOrEmpty(token) && IsValidJwtFormat(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - try refresh token first
        if (response.StatusCode == HttpStatusCode.Unauthorized && !request.RequestUri?.AbsolutePath.Contains("/auth/refresh") == true)
        {
            // Try to refresh the token
            var refreshSucceeded = await TryRefreshTokenAsync();
            
            if (refreshSucceeded)
            {
                // Retry the original request with new token
                var newToken = await _localStorage.GetItemAsync<string>("authToken") ?? 
                              await _localStorage.GetSessionItemAsync<string>("authToken");
                
                if (!string.IsNullOrEmpty(newToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            else
            {
                // Refresh failed - redirect to session expired page
                await HandleSessionExpiredAsync();
            }
        }

        return response;
    }

    private bool IsValidJwtFormat(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
            
        var parts = token.Split('.');
        return parts.Length == 3 && 
               !string.IsNullOrEmpty(parts[0]) && 
               !string.IsNullOrEmpty(parts[1]) && 
               !string.IsNullOrEmpty(parts[2]);
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken") ?? 
                              await _localStorage.GetSessionItemAsync<string>("refreshToken");
            
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            // Get base URL from configuration
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5002";
            
            var refreshRequest = new
            {
                RefreshToken = refreshToken
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(refreshRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{baseUrl}/api/auth/refresh", requestContent);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Success == true && result.Data != null)
                {
                    // Update tokens in storage
                    var isRememberMe = await _localStorage.GetItemAsync<string>("authToken") != null;
                    
                    if (isRememberMe)
                    {
                        await _localStorage.SetItemAsync("authToken", result.Data.AccessToken);
                        await _localStorage.SetItemAsync("refreshToken", result.Data.RefreshToken);
                    }
                    else
                    {
                        await _localStorage.SetSessionItemAsync("authToken", result.Data.AccessToken);
                        await _localStorage.SetSessionItemAsync("refreshToken", result.Data.RefreshToken);
                    }

                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleSessionExpiredAsync()
    {
        // Clear authentication data
        await ClearAuthenticationDataAsync();
        
        // Notify authentication state change
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsLoggedOut();
        }

        // Navigate to session expired page
        var navigationManager = _serviceProvider.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/session-expired", forceLoad: true);
    }

    private async Task ClearAuthenticationDataAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("user");
        await _localStorage.RemoveSessionItemAsync("authToken");
        await _localStorage.RemoveSessionItemAsync("refreshToken");
        await _localStorage.RemoveSessionItemAsync("user");
    }
}