using System.Text.Json;
using frontend.Models;
using Microsoft.Extensions.Logging;

namespace frontend.Services;

public class FavoritesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FavoritesService> _logger;

    public FavoritesService(HttpClient httpClient, ILogger<FavoritesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<List<FavoriteDto>>> GetUserFavoritesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/favorites");
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<FavoriteDto>>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<List<FavoriteDto>>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to get user favorites. Status: {StatusCode}, Content: {Content}", response.StatusCode, jsonContent);
            return ApiResponse<List<FavoriteDto>>.CreateFailure($"Failed to get favorites: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user favorites");
            return ApiResponse<List<FavoriteDto>>.CreateFailure("Failed to get favorites");
        }
    }

    public async Task<ApiResponse<FavoriteStatusDto>> CheckFavoriteStatusAsync(Guid toolId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/favorites/status/{toolId}");
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteStatusDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<FavoriteStatusDto>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to check favorite status for tool {ToolId}. Status: {StatusCode}, Content: {Content}", toolId, response.StatusCode, jsonContent);
            return ApiResponse<FavoriteStatusDto>.CreateFailure($"Failed to check favorite status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking favorite status for tool {ToolId}", toolId);
            return ApiResponse<FavoriteStatusDto>.CreateFailure("Failed to check favorite status");
        }
    }

    public async Task<ApiResponse<FavoriteDto>> AddToFavoritesAsync(Guid toolId)
    {
        try
        {
            var request = new AddToFavoritesRequest { ToolId = toolId.ToString() };
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/favorites", content);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<FavoriteDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<FavoriteDto>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to add tool {ToolId} to favorites. Status: {StatusCode}, Content: {Content}", toolId, response.StatusCode, jsonContent);
            return ApiResponse<FavoriteDto>.CreateFailure($"Failed to add to favorites: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tool {ToolId} to favorites", toolId);
            return ApiResponse<FavoriteDto>.CreateFailure("Failed to add to favorites");
        }
    }

    public async Task<ApiResponse<bool>> RemoveFromFavoritesAsync(Guid toolId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/favorites/tool/{toolId}");
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to remove tool {ToolId} from favorites. Status: {StatusCode}, Content: {Content}", toolId, response.StatusCode, jsonContent);
            return ApiResponse<bool>.CreateFailure($"Failed to remove from favorites: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tool {ToolId} from favorites", toolId);
            return ApiResponse<bool>.CreateFailure("Failed to remove from favorites");
        }
    }

    public async Task<ApiResponse<bool>> RemoveFromFavoritesByIdAsync(Guid favoriteId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/favorites/{favoriteId}");
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to remove favorite {FavoriteId}. Status: {StatusCode}, Content: {Content}", favoriteId, response.StatusCode, jsonContent);
            return ApiResponse<bool>.CreateFailure($"Failed to remove favorite: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite {FavoriteId}", favoriteId);
            return ApiResponse<bool>.CreateFailure("Failed to remove favorite");
        }
    }

    public async Task<ApiResponse<int>> GetFavoritesCountAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/favorites/count");
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<int>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return apiResponse ?? ApiResponse<int>.CreateFailure("Failed to deserialize response");
            }

            _logger.LogWarning("Failed to get favorites count. Status: {StatusCode}, Content: {Content}", response.StatusCode, jsonContent);
            return ApiResponse<int>.CreateFailure($"Failed to get favorites count: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites count");
            return ApiResponse<int>.CreateFailure("Failed to get favorites count");
        }
    }
}