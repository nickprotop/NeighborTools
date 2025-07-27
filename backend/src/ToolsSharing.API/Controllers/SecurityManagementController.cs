using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.API.Services;

namespace ToolsSharing.API.Controllers;

/// <summary>
/// Admin controller for managing security blocks and cleanup operations
/// </summary>
[ApiController]
[Route("api/admin/security")]
[Authorize(Roles = "Admin")]
public class SecurityManagementController : ControllerBase
{
    private readonly IBruteForceProtectionService _bruteForceService;
    private readonly SecurityCleanupBackgroundService _cleanupService;
    private readonly ILogger<SecurityManagementController> _logger;

    public SecurityManagementController(
        IBruteForceProtectionService bruteForceService,
        SecurityCleanupBackgroundService cleanupService,
        ILogger<SecurityManagementController> logger)
    {
        _bruteForceService = bruteForceService;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all currently blocked users
    /// </summary>
    [HttpGet("blocked-users")]
    public async Task<ActionResult<ApiResponse<List<BlockedUserInfo>>>> GetBlockedUsers()
    {
        try
        {
            var blockedUsers = await _bruteForceService.GetBlockedUsersAsync();
            return Ok(ApiResponse<List<BlockedUserInfo>>.CreateSuccess(
                blockedUsers, 
                $"Retrieved {blockedUsers.Count} blocked users"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocked users");
            return StatusCode(500, ApiResponse<List<BlockedUserInfo>>.CreateFailure("Failed to retrieve blocked users"));
        }
    }

    /// <summary>
    /// Get all currently blocked IP addresses
    /// </summary>
    [HttpGet("blocked-ips")]
    public async Task<ActionResult<ApiResponse<List<BlockedIPInfo>>>> GetBlockedIPs()
    {
        try
        {
            var blockedIPs = await _bruteForceService.GetBlockedIPsAsync();
            return Ok(ApiResponse<List<BlockedIPInfo>>.CreateSuccess(
                blockedIPs, 
                $"Retrieved {blockedIPs.Count} blocked IP addresses"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocked IPs");
            return StatusCode(500, ApiResponse<List<BlockedIPInfo>>.CreateFailure("Failed to retrieve blocked IPs"));
        }
    }

    /// <summary>
    /// Manually unblock a user account
    /// </summary>
    [HttpPost("unblock-user")]
    public async Task<ActionResult<ApiResponse<object>>> UnblockUser([FromBody] UnblockUserRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            await _bruteForceService.UnlockUserAccountAsync(request.UserEmail, adminUserId);
            
            _logger.LogInformation("Admin {AdminId} manually unblocked user {UserEmail}", adminUserId, request.UserEmail);
            
            return Ok(ApiResponse<object>.CreateSuccess(
                new { UserEmail = request.UserEmail, UnblockedAt = DateTime.UtcNow },
                $"User {request.UserEmail} has been unblocked successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {UserEmail}", request.UserEmail);
            return StatusCode(500, ApiResponse<object>.CreateFailure($"Failed to unblock user {request.UserEmail}"));
        }
    }

    /// <summary>
    /// Manually unblock an IP address
    /// </summary>
    [HttpPost("unblock-ip")]
    public async Task<ActionResult<ApiResponse<object>>> UnblockIP([FromBody] UnblockIPRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            await _bruteForceService.UnblockIpAddressAsync(request.IPAddress, adminUserId);
            
            _logger.LogInformation("Admin {AdminId} manually unblocked IP {IPAddress}", adminUserId, request.IPAddress);
            
            return Ok(ApiResponse<object>.CreateSuccess(
                new { IPAddress = request.IPAddress, UnblockedAt = DateTime.UtcNow },
                $"IP address {request.IPAddress} has been unblocked successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking IP {IPAddress}", request.IPAddress);
            return StatusCode(500, ApiResponse<object>.CreateFailure($"Failed to unblock IP {request.IPAddress}"));
        }
    }

    /// <summary>
    /// Get security cleanup status
    /// </summary>
    [HttpGet("cleanup-status")]
    public async Task<ActionResult<ApiResponse<SecurityCleanupStatusResponse>>> GetCleanupStatus()
    {
        try
        {
            var bruteForceStatus = await _bruteForceService.GetCleanupStatusAsync();
            var serviceStatus = _cleanupService.GetStatus();
            
            var response = new SecurityCleanupStatusResponse
            {
                ServiceStatus = serviceStatus,
                SecurityStatus = bruteForceStatus,
                OverallHealth = bruteForceStatus.IsHealthy && serviceStatus.IsRunning,
                LastCheck = DateTime.UtcNow
            };

            return Ok(ApiResponse<SecurityCleanupStatusResponse>.CreateSuccess(
                response,
                $"Cleanup status retrieved - {(response.OverallHealth ? "Healthy" : "Issues detected")}"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cleanup status");
            return StatusCode(500, ApiResponse<SecurityCleanupStatusResponse>.CreateFailure("Failed to retrieve cleanup status"));
        }
    }

    /// <summary>
    /// Force immediate cleanup of expired blocks
    /// </summary>
    [HttpPost("force-cleanup")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> ForceCleanup()
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startTime = DateTime.UtcNow;
            
            // Get status before cleanup
            var beforeStatus = await _bruteForceService.GetCleanupStatusAsync();
            
            // Run cleanup
            await _bruteForceService.CleanupExpiredDataAsync();
            
            // Get status after cleanup
            var afterStatus = await _bruteForceService.GetCleanupStatusAsync();
            
            var result = new CleanupResult
            {
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                ExpiredPatternsFoundBefore = beforeStatus.ExpiredPatternsFound,
                ExpiredPatternsFoundAfter = afterStatus.ExpiredPatternsFound,
                PatternsUnblocked = beforeStatus.ExpiredPatternsFound - afterStatus.ExpiredPatternsFound,
                TotalActiveBlocksBefore = beforeStatus.TotalActiveBlocks,
                TotalActiveBlocksAfter = afterStatus.TotalActiveBlocks,
                TriggeredBy = adminUserId ?? "Admin"
            };
            
            _logger.LogInformation("Admin {AdminId} forced security cleanup - unblocked {UnblockedCount} patterns", 
                adminUserId, result.PatternsUnblocked);
            
            return Ok(ApiResponse<CleanupResult>.CreateSuccess(
                result,
                $"Cleanup completed - {result.PatternsUnblocked} patterns unblocked"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forced cleanup");
            return StatusCode(500, ApiResponse<CleanupResult>.CreateFailure("Failed to execute cleanup"));
        }
    }

    /// <summary>
    /// Get security statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<BruteForceStatistics>>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var statistics = await _bruteForceService.GetStatisticsAsync(startDate, endDate);
            
            return Ok(ApiResponse<BruteForceStatistics>.CreateSuccess(
                statistics,
                "Security statistics retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security statistics");
            return StatusCode(500, ApiResponse<BruteForceStatistics>.CreateFailure("Failed to retrieve security statistics"));
        }
    }
}

// Request/Response DTOs
public class UnblockUserRequest
{
    public string UserEmail { get; set; } = string.Empty;
}

public class UnblockIPRequest
{
    public string IPAddress { get; set; } = string.Empty;
}

public class SecurityCleanupStatusResponse
{
    public SecurityCleanupServiceStatus ServiceStatus { get; set; } = new();
    public SecurityCleanupStatus SecurityStatus { get; set; } = new();
    public bool OverallHealth { get; set; }
    public DateTime LastCheck { get; set; }
}

public class CleanupResult
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int ExpiredPatternsFoundBefore { get; set; }
    public int ExpiredPatternsFoundAfter { get; set; }
    public int PatternsUnblocked { get; set; }
    public int TotalActiveBlocksBefore { get; set; }
    public int TotalActiveBlocksAfter { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public TimeSpan Duration => CompletedAt - StartedAt;
}