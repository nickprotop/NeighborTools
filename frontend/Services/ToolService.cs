using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace frontend.Services;

public interface IToolService
{
    Task<ApiResponse<List<Tool>>> GetToolsAsync();
    Task<ApiResponse<PagedResult<Tool>>> GetToolsPagedAsync(int page = 1, int pageSize = 24, string? category = null, string? searchTerm = null, string? sortBy = null, decimal? maxDailyRate = null, bool? availableOnly = null);
    Task<ApiResponse<List<Tool>>> GetMyToolsAsync();
    Task<ApiResponse<Tool>> GetToolAsync(string id);
    Task<ApiResponse<Tool>> CreateToolAsync(CreateToolRequest request);
    Task<ApiResponse<Tool>> UpdateToolAsync(string id, UpdateToolRequest request);
    Task<ApiResponse> DeleteToolAsync(string id);
    Task<ApiResponse<List<Tool>>> SearchToolsAsync(string query);
    Task<ApiResponse<ToolRentalPreferences>> GetToolRentalPreferencesAsync(string toolId);
    Task<ApiResponse<List<string>>> UploadImagesAsync(List<IBrowserFile> files);
    
    // New feature methods
    Task<ApiResponse> IncrementViewCountAsync(string toolId);
    Task<ApiResponse<PagedResult<ToolReview>>> GetToolReviewsAsync(string toolId, int page = 1, int pageSize = 10);
    Task<ApiResponse<ToolReview>> CreateToolReviewAsync(string toolId, CreateToolReviewRequest request);
    Task<ApiResponse<ToolReviewSummaryDto>> GetToolReviewSummaryAsync(string toolId);
    Task<ApiResponse<bool>> CanUserReviewToolAsync(string toolId);
    Task<ApiResponse<List<Tool>>> GetFeaturedToolsAsync(int count = 6);
    Task<ApiResponse<List<Tool>>> GetPopularToolsAsync(int count = 6);
    Task<ApiResponse<List<TagDto>>> GetPopularTagsAsync(int count = 20);
    Task<ApiResponse<PagedResult<Tool>>> SearchToolsAdvancedAsync(ToolSearchRequest request);
    Task<ApiResponse> RequestApprovalAsync(string toolId, RequestApprovalRequest request);
}

public class ToolService : IToolService
{
    private readonly HttpClient _httpClient;

    public ToolService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<List<Tool>>> GetToolsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tools");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PagedResult<Tool>>> GetToolsPagedAsync(int page = 1, int pageSize = 24, string? category = null, string? searchTerm = null, string? sortBy = null, decimal? maxDailyRate = null, bool? availableOnly = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={page}",
                $"PageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(category))
                queryParams.Add($"Category={Uri.EscapeDataString(category)}");

            if (!string.IsNullOrEmpty(searchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(searchTerm)}");

            if (!string.IsNullOrEmpty(sortBy))
                queryParams.Add($"SortBy={Uri.EscapeDataString(sortBy)}");

            if (maxDailyRate.HasValue)
                queryParams.Add($"MaxDailyRate={maxDailyRate.Value}");

            if (availableOnly.HasValue)
                queryParams.Add($"AvailableOnly={availableOnly.Value}");

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/tools/paged?{queryString}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<PagedResult<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResult<Tool>>
            { 
                Success = false, 
                Message = $"Failed to retrieve tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> GetMyToolsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tools/my-tools");
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<List<Tool>> 
                    { 
                        Success = false, 
                        Message = "You must be logged in to view your tools. Please log in and try again." 
                    };
                }
                
                return new ApiResponse<List<Tool>> 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {content}" 
                };
            }
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve my tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> GetToolAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> CreateToolAsync(CreateToolRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tools", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to create tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<Tool>> UpdateToolAsync(string id, UpdateToolRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/tools/{id}", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<Tool>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<Tool> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Tool> 
            { 
                Success = false, 
                Message = $"Failed to update tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> DeleteToolAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/tools/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to delete tool: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> SearchToolsAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/search?query={Uri.EscapeDataString(query)}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to search tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<ToolRentalPreferences>> GetToolRentalPreferencesAsync(string toolId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{toolId}/rental-preferences");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<ToolRentalPreferences>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<ToolRentalPreferences> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ToolRentalPreferences> 
            { 
                Success = false, 
                Message = $"Failed to retrieve rental preferences: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<string>>> UploadImagesAsync(List<IBrowserFile> files)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(5 * 1024 * 1024)); // 5MB limit
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }

            var response = await _httpClient.PostAsync("/api/tools/upload-images", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<string>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<string>> 
            { 
                Success = false, 
                Message = $"Failed to upload images: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> IncrementViewCountAsync(string toolId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/tools/{toolId}/views", null);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to increment view count: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PagedResult<ToolReview>>> GetToolReviewsAsync(string toolId, int page = 1, int pageSize = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{toolId}/reviews?page={page}&pageSize={pageSize}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<ToolReview>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<PagedResult<ToolReview>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResult<ToolReview>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tool reviews: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<ToolReview>> CreateToolReviewAsync(string toolId, CreateToolReviewRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/tools/{toolId}/reviews", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<ToolReview>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<ToolReview> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ToolReview> 
            { 
                Success = false, 
                Message = $"Failed to create tool review: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<ToolReviewSummaryDto>> GetToolReviewSummaryAsync(string toolId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{toolId}/reviews/summary");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<ToolReviewSummaryDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<ToolReviewSummaryDto> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ToolReviewSummaryDto> 
            { 
                Success = false, 
                Message = $"Failed to retrieve tool review summary: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> GetFeaturedToolsAsync(int count = 6)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/featured?count={count}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve featured tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<Tool>>> GetPopularToolsAsync(int count = 6)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/popular?count={count}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve popular tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<List<TagDto>>> GetPopularTagsAsync(int count = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/tags?count={count}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<List<TagDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<List<TagDto>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<TagDto>> 
            { 
                Success = false, 
                Message = $"Failed to retrieve popular tags: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PagedResult<Tool>>> SearchToolsAdvancedAsync(ToolSearchRequest request)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(request.Query))
                queryParams.Add($"query={Uri.EscapeDataString(request.Query)}");
            if (!string.IsNullOrEmpty(request.Category))
                queryParams.Add($"category={Uri.EscapeDataString(request.Category)}");
            if (!string.IsNullOrEmpty(request.Tags))
                queryParams.Add($"tags={Uri.EscapeDataString(request.Tags)}");
            if (request.MinPrice.HasValue)
                queryParams.Add($"minPrice={request.MinPrice}");
            if (request.MaxPrice.HasValue)
                queryParams.Add($"maxPrice={request.MaxPrice}");
            if (!string.IsNullOrEmpty(request.Location))
                queryParams.Add($"location={Uri.EscapeDataString(request.Location)}");
            if (request.IsAvailable.HasValue)
                queryParams.Add($"isAvailable={request.IsAvailable}");
            if (request.IsFeatured.HasValue)
                queryParams.Add($"isFeatured={request.IsFeatured}");
            if (request.MinRating.HasValue)
                queryParams.Add($"minRating={request.MinRating}");
            if (!string.IsNullOrEmpty(request.SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");
            
            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/tools/search?{queryString}");
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<Tool>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse<PagedResult<Tool>> { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResult<Tool>> 
            { 
                Success = false, 
                Message = $"Failed to search tools: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<bool>> CanUserReviewToolAsync(string toolId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/tools/{toolId}/reviews/can-review");
            var content = await response.Content.ReadAsStringAsync();
            
            // Always try to deserialize the response, even for non-success status codes
            // The backend returns structured ApiResponse even for 400 errors
            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<bool> { Success = false, Message = "Invalid response" };
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return the raw content as the error message
                return new ApiResponse<bool> 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {content}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> 
            { 
                Success = false, 
                Message = $"Failed to check review eligibility: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse> RequestApprovalAsync(string toolId, RequestApprovalRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/tools/{toolId}/request-approval", request);
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ApiResponse 
            { 
                Success = false, 
                Message = $"Failed to request approval: {ex.Message}" 
            };
        }
    }
}