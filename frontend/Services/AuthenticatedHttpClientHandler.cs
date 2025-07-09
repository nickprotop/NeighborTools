using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;

namespace frontend.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthenticatedHttpClientHandler(ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
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

        // Handle 401 Unauthorized - token might be expired or invalid
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Clear authentication state to force re-login
            await ClearAuthenticationDataAsync();
            
            // Notify authentication state change
            if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                customProvider.MarkUserAsLoggedOut();
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