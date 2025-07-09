using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using frontend.Models;

namespace frontend.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken") ?? 
                       await _localStorage.GetSessionItemAsync<string>("authToken");
            var user = await _localStorage.GetItemAsync<UserInfo>("user") ?? 
                      await _localStorage.GetSessionItemAsync<UserInfo>("user");

            // Both token and user must be present for authentication
            if (string.IsNullOrEmpty(token) || user == null)
            {
                // Clean up inconsistent state - if one exists but not the other
                if (!string.IsNullOrEmpty(token) || user != null)
                {
                    await ClearAuthenticationDataAsync();
                }
                return new AuthenticationState(_anonymous);
            }

            // Validate token format (basic check)
            if (!IsValidJwtFormat(token))
            {
                await ClearAuthenticationDataAsync();
                return new AuthenticationState(_anonymous);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id),
                new("FirstName", user.FirstName),
                new("LastName", user.LastName)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            await ClearAuthenticationDataAsync();
            return new AuthenticationState(_anonymous);
        }
    }

    public void MarkUserAsAuthenticated(UserInfo user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
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

    private bool IsValidJwtFormat(string token)
    {
        // Basic JWT format validation - should have 3 parts separated by dots
        if (string.IsNullOrEmpty(token))
            return false;
            
        var parts = token.Split('.');
        return parts.Length == 3 && 
               !string.IsNullOrEmpty(parts[0]) && 
               !string.IsNullOrEmpty(parts[1]) && 
               !string.IsNullOrEmpty(parts[2]);
    }
}