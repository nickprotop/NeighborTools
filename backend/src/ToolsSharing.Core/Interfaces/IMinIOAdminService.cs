using ToolsSharing.Core.DTOs.Admin;

namespace ToolsSharing.Core.Interfaces;

/// <summary>
/// Service for MinIO administration, monitoring, and analytics
/// </summary>
public interface IMinIOAdminService
{
    /// <summary>
    /// Get comprehensive MinIO server statistics
    /// </summary>
    Task<MinIOServerStatsDto> GetServerStatsAsync();
    
    /// <summary>
    /// Get bucket information and statistics
    /// </summary>
    Task<List<MinIOBucketStatsDto>> GetBucketStatsAsync();
    
    /// <summary>
    /// Get storage usage analytics over time periods
    /// </summary>
    Task<MinIOStorageAnalyticsDto> GetStorageAnalyticsAsync(TimeSpan period);
    
    /// <summary>
    /// Get file upload/download activity metrics
    /// </summary>
    Task<MinIOActivityMetricsDto> GetActivityMetricsAsync(TimeSpan period);
    
    /// <summary>
    /// Get health status of MinIO service
    /// </summary>
    Task<MinIOHealthStatusDto> GetHealthStatusAsync();
    
    /// <summary>
    /// Get disk usage and capacity information
    /// </summary>
    Task<MinIODiskUsageDto> GetDiskUsageAsync();
    
    /// <summary>
    /// Get recent file operations (uploads, downloads, deletes)
    /// </summary>
    Task<List<MinIOFileOperationDto>> GetRecentFileOperationsAsync(int limit = 100);
    
    /// <summary>
    /// Get file distribution by type and size
    /// </summary>
    Task<MinIOFileDistributionDto> GetFileDistributionAsync();
    
    /// <summary>
    /// Cleanup orphaned files and optimize storage
    /// </summary>
    Task<MinIOCleanupResultDto> CleanupOrphanedFilesAsync();
    
    /// <summary>
    /// Backup bucket contents
    /// </summary>
    Task<MinIOBackupResultDto> BackupBucketAsync(string bucketName);
}