using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.DTOs.Admin;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class MinIOAdminController : ControllerBase
{
    private readonly IMinIOAdminService _minioAdminService;
    private readonly ILogger<MinIOAdminController> _logger;

    public MinIOAdminController(
        IMinIOAdminService minioAdminService,
        ILogger<MinIOAdminController> logger)
    {
        _minioAdminService = minioAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive MinIO server statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<MinIOServerStatsDto>>> GetServerStats()
    {
        try
        {
            var stats = await _minioAdminService.GetServerStatsAsync();
            
            return Ok(new ApiResponse<MinIOServerStatsDto>
            {
                Success = true,
                Data = stats,
                Message = "MinIO server statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MinIO server statistics");
            return StatusCode(500, new ApiResponse<MinIOServerStatsDto>
            {
                Success = false,
                Message = "Failed to retrieve server statistics",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get bucket information and statistics
    /// </summary>
    [HttpGet("buckets")]
    public async Task<ActionResult<ApiResponse<List<MinIOBucketStatsDto>>>> GetBucketStats()
    {
        try
        {
            var bucketStats = await _minioAdminService.GetBucketStatsAsync();
            
            return Ok(new ApiResponse<List<MinIOBucketStatsDto>>
            {
                Success = true,
                Data = bucketStats,
                Message = "Bucket statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bucket statistics");
            return StatusCode(500, new ApiResponse<List<MinIOBucketStatsDto>>
            {
                Success = false,
                Message = "Failed to retrieve bucket statistics",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get storage usage analytics over time periods
    /// </summary>
    [HttpGet("analytics/storage")]
    public async Task<ActionResult<ApiResponse<MinIOStorageAnalyticsDto>>> GetStorageAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var period = TimeSpan.FromDays(Math.Max(1, Math.Min(365, days))); // Limit between 1-365 days
            var analytics = await _minioAdminService.GetStorageAnalyticsAsync(period);
            
            return Ok(new ApiResponse<MinIOStorageAnalyticsDto>
            {
                Success = true,
                Data = analytics,
                Message = $"Storage analytics for {days} days retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage analytics for {Days} days", days);
            return StatusCode(500, new ApiResponse<MinIOStorageAnalyticsDto>
            {
                Success = false,
                Message = "Failed to retrieve storage analytics",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get file upload/download activity metrics
    /// </summary>
    [HttpGet("analytics/activity")]
    public async Task<ActionResult<ApiResponse<MinIOActivityMetricsDto>>> GetActivityMetrics([FromQuery] int days = 30)
    {
        try
        {
            var period = TimeSpan.FromDays(Math.Max(1, Math.Min(365, days))); // Limit between 1-365 days
            var metrics = await _minioAdminService.GetActivityMetricsAsync(period);
            
            return Ok(new ApiResponse<MinIOActivityMetricsDto>
            {
                Success = true,
                Data = metrics,
                Message = $"Activity metrics for {days} days retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity metrics for {Days} days", days);
            return StatusCode(500, new ApiResponse<MinIOActivityMetricsDto>
            {
                Success = false,
                Message = "Failed to retrieve activity metrics",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get health status of MinIO service
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<MinIOHealthStatusDto>>> GetHealthStatus()
    {
        try
        {
            var health = await _minioAdminService.GetHealthStatusAsync();
            
            return Ok(new ApiResponse<MinIOHealthStatusDto>
            {
                Success = true,
                Data = health,
                Message = "MinIO health status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MinIO health status");
            return StatusCode(500, new ApiResponse<MinIOHealthStatusDto>
            {
                Success = false,
                Message = "Failed to retrieve health status",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get disk usage and capacity information
    /// </summary>
    [HttpGet("disk-usage")]
    public async Task<ActionResult<ApiResponse<MinIODiskUsageDto>>> GetDiskUsage()
    {
        try
        {
            var diskUsage = await _minioAdminService.GetDiskUsageAsync();
            
            return Ok(new ApiResponse<MinIODiskUsageDto>
            {
                Success = true,
                Data = diskUsage,
                Message = "Disk usage information retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disk usage information");
            return StatusCode(500, new ApiResponse<MinIODiskUsageDto>
            {
                Success = false,
                Message = "Failed to retrieve disk usage information",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get recent file operations (uploads, downloads, deletes)
    /// </summary>
    [HttpGet("operations")]
    public async Task<ActionResult<ApiResponse<List<MinIOFileOperationDto>>>> GetRecentFileOperations([FromQuery] int limit = 100)
    {
        try
        {
            var limitClamped = Math.Max(1, Math.Min(1000, limit)); // Limit between 1-1000
            var operations = await _minioAdminService.GetRecentFileOperationsAsync(limitClamped);
            
            return Ok(new ApiResponse<List<MinIOFileOperationDto>>
            {
                Success = true,
                Data = operations,
                Message = $"Recent {operations.Count} file operations retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent file operations with limit {Limit}", limit);
            return StatusCode(500, new ApiResponse<List<MinIOFileOperationDto>>
            {
                Success = false,
                Message = "Failed to retrieve file operations",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get file distribution by type and size
    /// </summary>
    [HttpGet("distribution")]
    public async Task<ActionResult<ApiResponse<MinIOFileDistributionDto>>> GetFileDistribution()
    {
        try
        {
            var distribution = await _minioAdminService.GetFileDistributionAsync();
            
            return Ok(new ApiResponse<MinIOFileDistributionDto>
            {
                Success = true,
                Data = distribution,
                Message = "File distribution analysis retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file distribution analysis");
            return StatusCode(500, new ApiResponse<MinIOFileDistributionDto>
            {
                Success = false,
                Message = "Failed to retrieve file distribution analysis",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Cleanup orphaned files and optimize storage
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<ApiResponse<MinIOCleanupResultDto>>> CleanupOrphanedFiles()
    {
        try
        {
            _logger.LogInformation("Admin initiated MinIO cleanup operation");
            var result = await _minioAdminService.CleanupOrphanedFilesAsync();
            
            return Ok(new ApiResponse<MinIOCleanupResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Success 
                    ? $"Cleanup completed successfully. {result.FilesDeleted} files deleted, {result.SpaceReclaimedFormatted} reclaimed"
                    : "Cleanup operation encountered errors",
                Errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MinIO cleanup operation");
            return StatusCode(500, new ApiResponse<MinIOCleanupResultDto>
            {
                Success = false,
                Message = "Failed to perform cleanup operation",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Backup bucket contents
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult<ApiResponse<MinIOBackupResultDto>>> BackupBucket([FromQuery] string bucketName = "")
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                bucketName = "toolssharing-files"; // Default bucket
            }

            _logger.LogInformation("Admin initiated backup for bucket: {BucketName}", bucketName);
            var result = await _minioAdminService.BackupBucketAsync(bucketName);
            
            return Ok(new ApiResponse<MinIOBackupResultDto>
            {
                Success = result.Success,
                Data = result,
                Message = result.Success 
                    ? $"Backup completed successfully. {result.FilesBackedUp} files backed up ({result.TotalSizeFormatted})"
                    : "Backup operation encountered errors",
                Errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bucket backup for {BucketName}", bucketName);
            return StatusCode(500, new ApiResponse<MinIOBackupResultDto>
            {
                Success = false,
                Message = "Failed to perform backup operation",
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get comprehensive dashboard data in a single request
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<MinIODashboardDto>>> GetDashboardData()
    {
        try
        {
            _logger.LogInformation("Retrieving MinIO dashboard data");

            // Gather all dashboard data in parallel for better performance
            var statsTask = _minioAdminService.GetServerStatsAsync();
            var bucketsTask = _minioAdminService.GetBucketStatsAsync();
            var healthTask = _minioAdminService.GetHealthStatusAsync();
            var diskUsageTask = _minioAdminService.GetDiskUsageAsync();
            var recentOperationsTask = _minioAdminService.GetRecentFileOperationsAsync(10); // Last 10 operations

            await Task.WhenAll(statsTask, bucketsTask, healthTask, diskUsageTask, recentOperationsTask);

            var dashboardData = new MinIODashboardDto
            {
                ServerStats = await statsTask,
                BucketStats = await bucketsTask,
                HealthStatus = await healthTask,
                DiskUsage = await diskUsageTask,
                RecentOperations = await recentOperationsTask,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(new ApiResponse<MinIODashboardDto>
            {
                Success = true,
                Data = dashboardData,
                Message = "MinIO dashboard data retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MinIO dashboard data");
            return StatusCode(500, new ApiResponse<MinIODashboardDto>
            {
                Success = false,
                Message = "Failed to retrieve dashboard data",
                Errors = { ex.Message }
            });
        }
    }
}

