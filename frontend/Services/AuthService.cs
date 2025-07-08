using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using frontend.Models;

namespace frontend.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResult>> LoginAsync(LoginRequest request, bool rememberMe = false);
    Task<ApiResponse<AuthResult>> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserInfo?> GetCurrentUserAsync();
    Task RestoreAuthenticationAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<ApiResponse<AuthResult>> LoginAsync(LoginRequest request, bool rememberMe = false)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                
                var userInfo = new UserInfo
                {
                    Id = result.Data.UserId,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName
                };
                
                if (rememberMe)
                {
                    await _localStorage.SetItemAsync("authToken", result.Data.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", result.Data.RefreshToken);
                    await _localStorage.SetItemAsync("user", userInfo);
                }
                else
                {
                    await _localStorage.SetSessionItemAsync("authToken", result.Data.AccessToken);
                    await _localStorage.SetSessionItemAsync("refreshToken", result.Data.RefreshToken);
                    await _localStorage.SetSessionItemAsync("user", userInfo);
                }
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Data.AccessToken);
                
                ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(userInfo);
            }

            return result ?? new ApiResponse<AuthResult> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResult> 
            { 
                Success = false, 
                Message = $"Login failed: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<AuthResult>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                var userInfo = new UserInfo
                {
                    Id = result.Data.UserId,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName
                };
                
                await _localStorage.SetItemAsync("authToken", result.Data.AccessToken);
                await _localStorage.SetItemAsync("refreshToken", result.Data.RefreshToken);
                await _localStorage.SetItemAsync("user", userInfo);
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Data.AccessToken);
                
                ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(userInfo);
            }

            return result ?? new ApiResponse<AuthResult> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResult> 
            { 
                Success = false, 
                Message = $"Registration failed: {ex.Message}" 
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("user");
        await _localStorage.RemoveSessionItemAsync("authToken");
        await _localStorage.RemoveSessionItemAsync("refreshToken");
        await _localStorage.RemoveSessionItemAsync("user");
        
        _httpClient.DefaultRequestHeaders.Authorization = null;
        ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken") ?? 
                   await _localStorage.GetSessionItemAsync<string>("authToken");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        return await _localStorage.GetItemAsync<UserInfo>("user") ?? 
               await _localStorage.GetSessionItemAsync<UserInfo>("user");
    }

    public async Task RestoreAuthenticationAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken") ?? 
                   await _localStorage.GetSessionItemAsync<string>("authToken");
        var user = await _localStorage.GetItemAsync<UserInfo>("user") ?? 
                  await _localStorage.GetSessionItemAsync<UserInfo>("user");

        if (!string.IsNullOrEmpty(token) && user != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(user);
        }
    }
}