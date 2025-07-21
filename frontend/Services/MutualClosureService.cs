using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;

namespace ToolsSharing.Frontend.Services;

/// <summary>
/// Service for handling mutual dispute closure operations
/// Provides comprehensive functionality for creating, responding to, and managing mutual closure requests
/// </summary>
public class MutualClosureService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MutualClosureService> _logger;

    public MutualClosureService(HttpClient httpClient, ILogger<MutualClosureService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a dispute is eligible for mutual closure
    /// </summary>
    /// <param name="disputeId">The dispute ID to check</param>
    /// <returns>Eligibility information including reasons and restrictions</returns>
    public async Task<MutualClosureEligibilityDto?> CheckEligibilityAsync(Guid disputeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/disputes/{disputeId}/mutual-closure/eligibility");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MutualClosureEligibilityDto>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            _logger.LogWarning("Failed to check mutual closure eligibility. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking mutual closure eligibility for dispute {DisputeId}", disputeId);
            return null;
        }
    }

    /// <summary>
    /// Creates a new mutual closure request
    /// </summary>
    /// <param name="request">The mutual closure request details</param>
    /// <returns>Result of the creation operation</returns>
    public async Task<CreateMutualClosureResult?> CreateMutualClosureAsync(CreateMutualClosureRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/disputes/mutual-closure", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CreateMutualClosureResult>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to create mutual closure request. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            return CreateMutualClosureResult.CreateFailure($"Request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mutual closure request");
            return CreateMutualClosureResult.CreateFailure("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Responds to a mutual closure request (accept or reject)
    /// </summary>
    /// <param name="mutualClosureId">The mutual closure request ID</param>
    /// <param name="request">The response details</param>
    /// <returns>Result of the response operation</returns>
    public async Task<RespondToMutualClosureResult?> RespondToMutualClosureAsync(Guid mutualClosureId, RespondToMutualClosureRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/disputes/mutual-closure/{mutualClosureId}/respond", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RespondToMutualClosureResult>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to respond to mutual closure request. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            return RespondToMutualClosureResult.CreateFailure($"Response failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to mutual closure request {MutualClosureId}", mutualClosureId);
            return RespondToMutualClosureResult.CreateFailure("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Cancels a mutual closure request (only by the initiator)
    /// </summary>
    /// <param name="mutualClosureId">The mutual closure request ID</param>
    /// <param name="reason">Reason for cancellation</param>
    /// <returns>Result of the cancellation operation</returns>
    public async Task<RespondToMutualClosureResult?> CancelMutualClosureAsync(Guid mutualClosureId, string reason)
    {
        try
        {
            var request = new CancelMutualClosureRequest 
            { 
                MutualClosureId = mutualClosureId,
                CancellationReason = reason 
            };
            
            var response = await _httpClient.PostAsJsonAsync($"/api/disputes/mutual-closure/{mutualClosureId}/cancel", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RespondToMutualClosureResult>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to cancel mutual closure request. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            return RespondToMutualClosureResult.CreateFailure($"Cancellation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling mutual closure request {MutualClosureId}", mutualClosureId);
            return RespondToMutualClosureResult.CreateFailure("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Gets all mutual closure requests for a specific dispute
    /// </summary>
    /// <param name="disputeId">The dispute ID</param>
    /// <returns>List of mutual closure requests</returns>
    public async Task<GetMutualClosuresResult?> GetDisputeMutualClosuresAsync(Guid disputeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/disputes/{disputeId}/mutual-closure");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GetMutualClosuresResult>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            _logger.LogWarning("Failed to get mutual closures for dispute. Status: {StatusCode}", response.StatusCode);
            return GetMutualClosuresResult.CreateFailure($"Request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mutual closures for dispute {DisputeId}", disputeId);
            return GetMutualClosuresResult.CreateFailure("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Gets a specific mutual closure request details
    /// </summary>
    /// <param name="mutualClosureId">The mutual closure request ID</param>
    /// <returns>Detailed mutual closure information</returns>
    public async Task<MutualClosureDto?> GetMutualClosureDetailsAsync(Guid mutualClosureId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/disputes/mutual-closure/{mutualClosureId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MutualClosureDto>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            _logger.LogWarning("Failed to get mutual closure details. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mutual closure details {MutualClosureId}", mutualClosureId);
            return null;
        }
    }

    /// <summary>
    /// Gets mutual closure requests for the current user
    /// </summary>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of user's mutual closure requests</returns>
    public async Task<GetMutualClosuresResult?> GetUserMutualClosuresAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/disputes/mutual-closure/user?page={page}&pageSize={pageSize}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GetMutualClosuresResult>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            _logger.LogWarning("Failed to get user mutual closures. Status: {StatusCode}", response.StatusCode);
            return GetMutualClosuresResult.CreateFailure($"Request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user mutual closures");
            return GetMutualClosuresResult.CreateFailure("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Gets all mutual closures for admin oversight (admin-only method)
    /// </summary>
    /// <returns>List of all mutual closure requests</returns>
    public async Task<ApiResponse<IEnumerable<MutualClosureSummaryDto>>?> GetAllMutualClosuresAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all mutual closures for admin oversight");
            
            var response = await _httpClient.GetAsync("/api/admin/mutual-closures");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<MutualClosureSummaryDto>>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully fetched {Count} mutual closures", result?.Data?.Count() ?? 0);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to fetch mutual closures: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<IEnumerable<MutualClosureSummaryDto>>
                {
                    Success = false,
                    Message = $"Failed to fetch mutual closures: {response.StatusCode}",
                    Data = Enumerable.Empty<MutualClosureSummaryDto>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all mutual closures");
            return new ApiResponse<IEnumerable<MutualClosureSummaryDto>>
            {
                Success = false,
                Message = "An error occurred while fetching mutual closures",
                Data = Enumerable.Empty<MutualClosureSummaryDto>()
            };
        }
    }

    /// <summary>
    /// Admin review of a mutual closure request (admin-only method)
    /// </summary>
    /// <param name="request">The admin review request</param>
    /// <returns>Result of the admin review operation</returns>
    public async Task<ApiResponse<MutualClosureDto>?> AdminReviewMutualClosureAsync(AdminReviewMutualClosureRequest request)
    {
        try
        {
            _logger.LogInformation("Admin reviewing mutual closure {MutualClosureId} with action {Action}", 
                request.MutualClosureId, request.Action);
            
            var response = await _httpClient.PostAsJsonAsync($"/api/admin/mutual-closure/{request.MutualClosureId}/review", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<MutualClosureDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully reviewed mutual closure {MutualClosureId}", request.MutualClosureId);
                return result;
            }
            else
            {
                _logger.LogWarning("Failed to review mutual closure: {StatusCode} - {Content}", response.StatusCode, content);
                return new ApiResponse<MutualClosureDto>
                {
                    Success = false,
                    Message = $"Admin review failed: {response.StatusCode}",
                    Data = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin review of mutual closure {MutualClosureId}", request.MutualClosureId);
            return new ApiResponse<MutualClosureDto>
            {
                Success = false,
                Message = "An error occurred during admin review",
                Data = null
            };
        }
    }

}