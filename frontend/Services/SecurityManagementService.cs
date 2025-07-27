using System.Net.Http.Json;
using System.Text.Json;
using frontend.Models;

namespace frontend.Services;

public class SecurityManagementService : ISecurityManagementService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SecurityManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ApiResponse<List<BlockedUserInfo>>> GetBlockedUsersAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/security/blocked-users");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<BlockedUserInfo>>>(_jsonOptions)
            ?? new ApiResponse<List<BlockedUserInfo>> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<List<BlockedIPInfo>>> GetBlockedIPsAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/security/blocked-ips");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<BlockedIPInfo>>>(_jsonOptions)
            ?? new ApiResponse<List<BlockedIPInfo>> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<object>> UnblockUserAsync(string userEmail)
    {
        var request = new { UserEmail = userEmail };
        var response = await _httpClient.PostAsJsonAsync("/api/admin/security/unblock-user", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions)
            ?? new ApiResponse<object> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<object>> UnblockIPAsync(string ipAddress)
    {
        var request = new { IPAddress = ipAddress };
        var response = await _httpClient.PostAsJsonAsync("/api/admin/security/unblock-ip", request, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions)
            ?? new ApiResponse<object> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<SecurityCleanupStatusResponse>> GetCleanupStatusAsync()
    {
        var response = await _httpClient.GetAsync("/api/admin/security/cleanup-status");
        return await response.Content.ReadFromJsonAsync<ApiResponse<SecurityCleanupStatusResponse>>(_jsonOptions)
            ?? new ApiResponse<SecurityCleanupStatusResponse> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<CleanupResult>> ForceCleanupAsync()
    {
        var response = await _httpClient.PostAsync("/api/admin/security/force-cleanup", null);
        return await response.Content.ReadFromJsonAsync<ApiResponse<CleanupResult>>(_jsonOptions)
            ?? new ApiResponse<CleanupResult> { Success = false, Message = "Failed to deserialize response" };
    }

    public async Task<ApiResponse<BruteForceStatistics>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/api/admin/security/statistics{queryString}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<BruteForceStatistics>>(_jsonOptions)
            ?? new ApiResponse<BruteForceStatistics> { Success = false, Message = "Failed to deserialize response" };
    }
}