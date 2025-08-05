using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using frontend.Models;
using Microsoft.AspNetCore.Components;
using ToolsSharing.Frontend.Configuration;

namespace frontend.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppSettings _appSettings;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    public AuthenticatedHttpClientHandler(
        ILocalStorageService localStorage, 
        AuthenticationStateProvider authStateProvider,
        IServiceProvider serviceProvider,
        AppSettings appSettings)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _serviceProvider = serviceProvider;
        _appSettings = appSettings;
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
        if (response.StatusCode == HttpStatusCode.Unauthorized && 
            !request.RequestUri?.AbsolutePath.Contains("/auth/refresh") == true &&
            !request.RequestUri?.AbsolutePath.Contains("/auth/login") == true)
        {
            // Use semaphore to prevent multiple concurrent refresh attempts
            await _refreshSemaphore.WaitAsync();
            try
            {
                // Double-check if token was already refreshed by another request
                var currentToken = await _localStorage.GetItemAsync<string>("authToken") ?? 
                                  await _localStorage.GetSessionItemAsync<string>("authToken");
                
                if (!string.IsNullOrEmpty(currentToken) && IsValidJwtFormat(currentToken) && currentToken != token)
                {
                    // Token was already refreshed by another request
                    Console.WriteLine("AuthenticatedHttpClientHandler: Token already refreshed by concurrent request");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                    response = await base.SendAsync(request, cancellationToken);
                    return response;
                }
                
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
                    // Refresh failed - but don't immediately clear tokens in case there are concurrent refresh attempts
                    // Only clear tokens if we can confirm the refresh token itself is invalid
                    Console.WriteLine("AuthenticatedHttpClientHandler: Token refresh failed, but not clearing tokens immediately to avoid race conditions");
                    
                    // Still need to handle the session expiry, but more gracefully
                    await HandleSessionExpiredGracefullyAsync();
                }
            }
            finally
            {
                _refreshSemaphore.Release();
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
            {
                Console.WriteLine("AuthenticatedHttpClientHandler: No refresh token found in storage");
                return false;
            }

            // Get base URL from AppSettings
            var baseUrl = _appSettings.ApiSettings.BaseUrl;
            
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

                    Console.WriteLine("AuthenticatedHttpClientHandler: Token refresh successful");
                    return true;
                }
                else
                {
                    Console.WriteLine($"AuthenticatedHttpClientHandler: Token refresh failed - API returned success=false: {result?.Message}");
                }
            }
            else
            {
                Console.WriteLine($"AuthenticatedHttpClientHandler: Token refresh failed - HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AuthenticatedHttpClientHandler: Token refresh exception: {ex.Message}");
            return false;
        }
    }

    private async Task HandleSessionExpiredGracefullyAsync()
    {
        // Check if this is a first visit (no auth data ever existed)
        var hadPreviousSession = await _localStorage.ContainsKeyAsync("authToken") ||
                                await _localStorage.ContainsKeyAsync("refreshToken") ||
                                await _localStorage.ContainsKeyAsync("user") ||
                                await _localStorage.ContainsSessionKeyAsync("authToken") ||
                                await _localStorage.ContainsSessionKeyAsync("refreshToken") ||
                                await _localStorage.ContainsSessionKeyAsync("user");

        // Only clear authentication data after a delay to avoid race conditions with concurrent refresh attempts
        // This gives other concurrent requests a chance to complete their refresh attempts
        await Task.Delay(1000); // 1 second delay
        
        // Double-check if refresh tokens still exist (another request might have successfully refreshed)
        var stillHasRefreshToken = await _localStorage.GetItemAsync<string>("refreshToken") != null ||
                                  await _localStorage.GetSessionItemAsync<string>("refreshToken") != null;
        
        if (stillHasRefreshToken)
        {
            // Another concurrent request successfully refreshed - don't clear tokens
            Console.WriteLine("AuthenticatedHttpClientHandler: Refresh token still exists, another request may have succeeded");
            return;
        }

        // Clear authentication data only if no successful refresh happened
        await ClearAuthenticationDataAsync();
        
        // Notify authentication state change
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsLoggedOut();
        }

        // Only redirect to session-expired if user had a previous session
        if (hadPreviousSession)
        {
            // User had a session that expired - show session expired page
            var navigationManager = _serviceProvider.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/session-expired", forceLoad: true);
        }
        // Otherwise, for first-time visitors, don't redirect anywhere
    }

    private async Task HandleSessionExpiredAsync()
    {
        // Immediate session expiry for cases where we're certain (like explicit logout)
        await ClearAuthenticationDataAsync();
        
        // Notify authentication state change
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsLoggedOut();
        }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshSemaphore?.Dispose();
            _httpClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}