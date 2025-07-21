using System.Net.Http.Json;
using System.Text.Json;
using ToolsSharing.Frontend.Models;
using frontend.Models;

namespace ToolsSharing.Frontend.Services;

public class BundleReviewService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BundleReviewService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ApiResponse<BundleReviewDto>> CreateBundleReviewAsync(Guid bundleId, CreateBundleReviewRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/bundles/{bundleId}/reviews", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<BundleReviewDto>>(content, _jsonOptions);
                return result ?? ApiResponse<BundleReviewDto>.CreateFailure("Failed to deserialize response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<BundleReviewDto>.CreateFailure($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return ApiResponse<BundleReviewDto>.CreateFailure($"Network error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PagedResult<BundleReviewDto>>> GetBundleReviewsAsync(Guid bundleId, int page = 1, int pageSize = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/bundles/{bundleId}/reviews?page={page}&pageSize={pageSize}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleReviewDto>>>(content, _jsonOptions);
                return result ?? ApiResponse<PagedResult<BundleReviewDto>>.CreateFailure("Failed to deserialize response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<PagedResult<BundleReviewDto>>.CreateFailure($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<BundleReviewDto>>.CreateFailure($"Network error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BundleReviewSummaryDto>> GetBundleReviewSummaryAsync(Guid bundleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/bundles/{bundleId}/reviews/summary");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<BundleReviewSummaryDto>>(content, _jsonOptions);
                return result ?? ApiResponse<BundleReviewSummaryDto>.CreateFailure("Failed to deserialize response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<BundleReviewSummaryDto>.CreateFailure($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return ApiResponse<BundleReviewSummaryDto>.CreateFailure($"Network error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> CanUserReviewBundleAsync(Guid bundleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/bundles/{bundleId}/can-review");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<bool>.CreateFailure($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteBundleReviewAsync(Guid reviewId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/bundles/reviews/{reviewId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
                return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<bool>.CreateFailure($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
        }
    }
}