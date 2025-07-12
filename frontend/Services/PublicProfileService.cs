using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public interface IPublicProfileService
{
    Task<ApiResponse<PublicUserProfileDto>?> GetPublicProfileAsync(string userId);
}

public class PublicProfileService : IPublicProfileService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PublicProfileService> _logger;

    public PublicProfileService(HttpClient httpClient, ILogger<PublicProfileService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<PublicUserProfileDto>?> GetPublicProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/users/public/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<PublicUserProfileDto>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            _logger.LogWarning("Failed to get public profile for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            return new ApiResponse<PublicUserProfileDto>
            {
                Success = false,
                Message = $"Failed to retrieve user profile: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while getting public profile for user {UserId}", userId);
            return new ApiResponse<PublicUserProfileDto>
            {
                Success = false,
                Message = "An error occurred while retrieving user profile",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}