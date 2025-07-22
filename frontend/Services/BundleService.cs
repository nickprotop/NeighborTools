using System.Text.Json;
using System.Net.Http.Json;
using ToolsSharing.Frontend.Models;
using frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ToolsSharing.Frontend.Services
{
    public class BundleService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public BundleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        // Bundle browsing and retrieval
        public async Task<ApiResponse<PagedResult<BundleModel>>> GetBundlesAsync(
            int page = 1, int pageSize = 20, string? category = null, 
            string? searchTerm = null, bool featuredOnly = false)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrEmpty(category))
                    queryParams.Add($"category={Uri.EscapeDataString(category)}");
                
                if (!string.IsNullOrEmpty(searchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
                
                if (featuredOnly)
                    queryParams.Add("featuredOnly=true");

                var queryString = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"api/bundles?{queryString}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleModel>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<BundleModel>>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleModel>>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<PagedResult<BundleModel>>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleModel>>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleModel?>> GetBundleByIdAsync(Guid bundleId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bundles/{bundleId}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(json, _jsonOptions);
                    return result ?? ApiResponse<BundleModel?>.CreateFailure("Failed to deserialize response");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return ApiResponse<BundleModel?>.CreateSuccess(null, "Bundle not found");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(json, _jsonOptions);
                return ApiResponse<BundleModel?>.CreateFailure(errorResult?.Message ?? "Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleModel?>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<BundleModel>>> GetFeaturedBundlesAsync(int count = 6)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bundles/featured?count={count}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<BundleModel>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<BundleModel>>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<List<BundleModel>>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<List<BundleModel>>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<BundleModel>>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetBundleCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/bundles/categories");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, int>>>(json, _jsonOptions);
                    return result ?? ApiResponse<Dictionary<string, int>>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, int>>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<Dictionary<string, int>>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<Dictionary<string, int>>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        // Bundle availability and cost calculation
        public async Task<ApiResponse<BundleAvailabilityResponseModel>> CheckBundleAvailabilityAsync(BundleAvailabilityModel request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/bundles/{request.BundleId}/availability", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleAvailabilityResponseModel>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<BundleAvailabilityResponseModel>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleAvailabilityResponseModel>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<BundleAvailabilityResponseModel>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleAvailabilityResponseModel>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleCostCalculationModel>> CalculateBundleCostAsync(Guid bundleId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var request = new BundleAvailabilityModel
                {
                    BundleId = bundleId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/bundles/{bundleId}/cost", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleCostCalculationModel>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<BundleCostCalculationModel>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleCostCalculationModel>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<BundleCostCalculationModel>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleCostCalculationModel>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        // Bundle management (authenticated)
        public async Task<ApiResponse<BundleModel>> CreateBundleAsync(CreateBundleModel bundle)
        {
            try
            {
                var json = JsonSerializer.Serialize(bundle, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/bundles", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<BundleModel>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<BundleModel>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleModel>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleModel>> UpdateBundleAsync(Guid bundleId, CreateBundleModel bundle)
        {
            try
            {
                var json = JsonSerializer.Serialize(bundle, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/bundles/{bundleId}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<BundleModel>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleModel>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<BundleModel>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleModel>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteBundleAsync(Guid bundleId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/bundles/{bundleId}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                    return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<bool>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleModel>>> GetMyBundlesAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bundles/my-bundles?page={page}&pageSize={pageSize}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleModel>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<BundleModel>>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleModel>>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<PagedResult<BundleModel>>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleModel>>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        // Bundle rental methods
        public async Task<ApiResponse<BundleRentalModel>> CreateBundleRentalAsync(CreateBundleRentalModel rental)
        {
            try
            {
                var json = JsonSerializer.Serialize(rental, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/bundles/rentals", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleRentalModel>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<BundleRentalModel>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleRentalModel>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<BundleRentalModel>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleRentalModel>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResult<BundleRentalModel>>> GetUserBundleRentalsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bundles/rentals?page={page}&pageSize={pageSize}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleRentalModel>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<BundleRentalModel>>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<PagedResult<BundleRentalModel>>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<PagedResult<BundleRentalModel>>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResult<BundleRentalModel>>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ApproveBundleRentalAsync(Guid rentalId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bundles/rentals/{rentalId}/approve", null);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                    return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<bool>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> RejectBundleRentalAsync(Guid rentalId, string reason)
        {
            try
            {
                var requestBody = new { reason };
                var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/bundles/rentals/{rentalId}/reject", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(responseJson, _jsonOptions);
                    return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<bool>>(responseJson, _jsonOptions);
                return errorResult ?? ApiResponse<bool>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelBundleRentalAsync(Guid rentalId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/bundles/rentals/{rentalId}/cancel", null);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                    return result ?? ApiResponse<bool>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<bool>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BundleApprovalStatusDto>> GetBundleApprovalStatusAsync(Guid bundleId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bundles/{bundleId}/approval-status");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BundleApprovalStatusDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<BundleApprovalStatusDto>.CreateFailure("Failed to deserialize response");
                }

                var errorResult = JsonSerializer.Deserialize<ApiResponse<BundleApprovalStatusDto>>(json, _jsonOptions);
                return errorResult ?? ApiResponse<BundleApprovalStatusDto>.CreateFailure("Request failed");
            }
            catch (Exception ex)
            {
                return ApiResponse<BundleApprovalStatusDto>.CreateFailure($"Network error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UploadBundleImageAsync(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(5 * 1024 * 1024)); // 5MB limit
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.Name);

                var response = await _httpClient.PostAsync("api/bundles/upload-image", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);
                return result ?? ApiResponse<string>.CreateFailure("Invalid response");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.CreateFailure($"Failed to upload image: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UploadBundleImageAsync(byte[] imageData, string contentType, string fileName)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageData);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(fileContent, "file", fileName);

                var response = await _httpClient.PostAsync("api/bundles/upload-image", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);
                return result ?? ApiResponse<string>.CreateFailure("Invalid response");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.CreateFailure($"Failed to upload image: {ex.Message}");
            }
        }

        /// <summary>
        /// Request approval for a rejected bundle
        /// </summary>
        public async Task<ApiResponse> RequestApprovalAsync(Guid bundleId, ToolsSharing.Frontend.Models.RequestApprovalRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/bundles/{bundleId}/request-approval", request, _jsonOptions);
                var content = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
                return result ?? new ApiResponse { Success = false, Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = $"Failed to request approval: {ex.Message}" };
            }
        }
    }
}