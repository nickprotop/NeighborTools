using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public class SettingsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(HttpClient httpClient, ILogger<SettingsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<UserSettingsDto>?> GetSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/settings");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            _logger.LogWarning("Failed to get settings. Status: {StatusCode}", response.StatusCode);
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = $"Failed to retrieve settings: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while getting settings");
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<UserSettingsDto>?> UpdateSettingsAsync(UpdateUserSettingsRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync("api/settings", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            var errorJson = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(errorJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            return errorResponse ?? new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = $"Failed to update settings: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating settings");
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<UserSettingsDto>?> UpdatePrivacySettingsAsync(PrivacySettingsDto privacy)
    {
        try
        {
            var json = JsonSerializer.Serialize(privacy, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync("api/settings/privacy", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            var errorJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(errorJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating privacy settings");
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating privacy settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<UserSettingsDto>?> UpdateNotificationSettingsAsync(NotificationSettingsDto notifications)
    {
        try
        {
            var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync("api/settings/notifications", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            var errorJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(errorJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating notification settings");
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while updating notification settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>?> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/settings/change-password", content);
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while changing password");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while changing password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<UserSettingsDto>?> ResetSettingsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/settings/reset", null);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            var errorJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<UserSettingsDto>>(errorJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while resetting settings");
            return new ApiResponse<UserSettingsDto>
            {
                Success = false,
                Message = "An error occurred while resetting settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}