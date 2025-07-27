using frontend.Models;

namespace frontend.Services;

public interface ISecurityManagementService
{
    Task<ApiResponse<List<BlockedUserInfo>>> GetBlockedUsersAsync();
    Task<ApiResponse<List<BlockedIPInfo>>> GetBlockedIPsAsync();
    Task<ApiResponse<object>> UnblockUserAsync(string userEmail);
    Task<ApiResponse<object>> UnblockIPAsync(string ipAddress);
    Task<ApiResponse<SecurityCleanupStatusResponse>> GetCleanupStatusAsync();
    Task<ApiResponse<CleanupResult>> ForceCleanupAsync();
    Task<ApiResponse<BruteForceStatistics>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}