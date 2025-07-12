using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using frontend.Models;

namespace frontend.Services;

public class UserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResponse<UserProfileDto>?> GetProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/user/profile");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(content, _jsonOptions);
            }
            
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Failed to get profile: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Error getting profile: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserProfileDto>?> UpdateProfileAsync(UpdateUserProfileRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/api/user/profile", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(content, _jsonOptions);
            }
            
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Failed to update profile: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Error updating profile: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserStatisticsDto>?> GetStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/user/statistics");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<UserStatisticsDto>>(content, _jsonOptions);
            }
            
            return new ApiResponse<UserStatisticsDto>
            {
                Success = false,
                Message = $"Failed to get statistics: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserStatisticsDto>
            {
                Success = false,
                Message = $"Error getting statistics: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<PagedResult<UserReviewDto>>?> GetReviewsAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/reviews?page={page}&pageSize={pageSize}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<PagedResult<UserReviewDto>>>(content, _jsonOptions);
            }
            
            return new ApiResponse<PagedResult<UserReviewDto>>
            {
                Success = false,
                Message = $"Failed to get reviews: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResult<UserReviewDto>>
            {
                Success = false,
                Message = $"Error getting reviews: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserProfileDto>?> GetPublicProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/profile/{userId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(content, _jsonOptions);
            }
            
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Failed to get public profile: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserProfileDto>
            {
                Success = false,
                Message = $"Error getting public profile: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<string>?> UploadProfilePictureAsync(IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("/api/user/profile-picture", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);
            }
            
            return new ApiResponse<string>
            {
                Success = false,
                Message = $"Failed to upload profile picture: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = $"Error uploading profile picture: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>?> RemoveProfilePictureAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/user/profile-picture");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
            }
            
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to remove profile picture: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error removing profile picture: {ex.Message}"
            };
        }
    }
}