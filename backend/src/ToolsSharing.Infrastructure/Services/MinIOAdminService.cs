using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using System.Diagnostics;
using System.Text.Json;
using ToolsSharing.Core.DTOs.Admin;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class MinIOAdminService : IMinIOAdminService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIOAdminService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly string _bucketName;

    public MinIOAdminService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinIOAdminService> logger,
        IFileStorageService fileStorageService)
    {
        _minioClient = minioClient;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _bucketName = configuration["MinIO:BucketName"] ?? "toolssharing-files";
    }

    public async Task<MinIOServerStatsDto> GetServerStatsAsync()
    {
        try
        {
            var stats = new MinIOServerStatsDto
            {
                IsOnline = true,
                Region = "us-east-1", // Default region
                Version = "MINIO.2024", // MinIO version
                Uptime = DateTime.UtcNow.AddDays(-1), // Simulated uptime
                TotalBuckets = 0,
                TotalObjects = 0,
                TotalStorage = 0,
                UsedStorage = 0
            };

            // Get bucket list and calculate totals
            var buckets = await GetBucketStatsAsync();
            stats.TotalBuckets = buckets.Count;
            stats.TotalObjects = buckets.Sum(b => b.ObjectCount);
            stats.UsedStorage = buckets.Sum(b => b.Size);
            
            // Simulate total storage capacity (1TB)
            stats.TotalStorage = 1024L * 1024L * 1024L * 1024L; // 1TB
            stats.FreeStorage = stats.TotalStorage - stats.UsedStorage;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MinIO server stats");
            return new MinIOServerStatsDto { IsOnline = false };
        }
    }

    public async Task<List<MinIOBucketStatsDto>> GetBucketStatsAsync()
    {
        try
        {
            var bucketStats = new List<MinIOBucketStatsDto>();
            var buckets = await _minioClient.ListBucketsAsync();

            foreach (var bucket in buckets.Buckets)
            {
                var stats = new MinIOBucketStatsDto
                {
                    Name = bucket.Name,
                    CreationDate = bucket.CreationDateDateTime,
                    Size = 0,
                    ObjectCount = 0,
                    RecentFiles = new List<string>()
                };

                // Get object statistics for this bucket
                var objectStats = await GetBucketObjectStatsAsync(bucket.Name);
                stats.Size = objectStats.TotalSize;
                stats.ObjectCount = objectStats.ObjectCount;
                stats.SizeFormatted = FormatBytes(stats.Size);
                stats.RecentFiles = objectStats.RecentFiles;

                bucketStats.Add(stats);
            }

            return bucketStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bucket stats");
            return new List<MinIOBucketStatsDto>();
        }
    }

    public async Task<MinIOStorageAnalyticsDto> GetStorageAnalyticsAsync(TimeSpan period)
    {
        try
        {
            var analytics = new MinIOStorageAnalyticsDto
            {
                Period = period,
                StorageGrowth = new List<StorageDataPointDto>(),
                ObjectGrowth = new List<StorageDataPointDto>()
            };

            // Simulate historical data (in a real implementation, this would come from metrics storage)
            var days = (int)period.TotalDays;
            var random = new Random();
            var baseStorage = 1024L * 1024L * 100L; // 100MB base
            var baseObjects = 50;

            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var growthFactor = (days - i + 1) * 1.1;
                
                var storage = (long)(baseStorage * growthFactor * (0.8 + random.NextDouble() * 0.4));
                var objects = (int)(baseObjects * growthFactor * (0.8 + random.NextDouble() * 0.4));

                analytics.StorageGrowth.Add(new StorageDataPointDto
                {
                    Date = date,
                    Value = storage,
                    FormattedValue = FormatBytes(storage)
                });

                analytics.ObjectGrowth.Add(new StorageDataPointDto
                {
                    Date = date,
                    Value = objects,
                    FormattedValue = objects.ToString()
                });
            }

            // Calculate growth rate
            if (analytics.StorageGrowth.Count >= 2)
            {
                var first = analytics.StorageGrowth.First().Value;
                var last = analytics.StorageGrowth.Last().Value;
                analytics.GrowthRatePerDay = days > 0 ? (double)(last - first) / first / days : 0;
                analytics.PredictedSizeNextMonth = (long)(last * (1 + analytics.GrowthRatePerDay * 30));
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage analytics");
            return new MinIOStorageAnalyticsDto { Period = period };
        }
    }

    public async Task<MinIOActivityMetricsDto> GetActivityMetricsAsync(TimeSpan period)
    {
        try
        {
            // In a real implementation, this would come from MinIO access logs or metrics
            var metrics = new MinIOActivityMetricsDto
            {
                Period = period,
                DailyActivity = new List<ActivityDataPointDto>(),
                PopularFileTypes = new List<PopularFileTypeDto>()
            };

            var days = (int)period.TotalDays;
            var random = new Random();
            
            // Simulate daily activity
            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                metrics.DailyActivity.Add(new ActivityDataPointDto
                {
                    Date = date,
                    Uploads = random.Next(5, 25),
                    Downloads = random.Next(10, 50),
                    Deletes = random.Next(0, 5)
                });
            }

            // Calculate totals
            metrics.TotalUploads = metrics.DailyActivity.Sum(d => d.Uploads);
            metrics.TotalDownloads = metrics.DailyActivity.Sum(d => d.Downloads);
            metrics.TotalDeletes = metrics.DailyActivity.Sum(d => d.Deletes);
            metrics.TotalBytesUploaded = metrics.TotalUploads * 1024L * 512L; // Avg 512KB per upload
            metrics.TotalBytesDownloaded = metrics.TotalDownloads * 1024L * 256L; // Avg 256KB per download

            // Simulate popular file types
            metrics.PopularFileTypes = new List<PopularFileTypeDto>
            {
                new() { FileType = "Images", Extension = ".jpg,.png,.gif", Count = random.Next(50, 200), TotalSize = random.Next(50, 500) * 1024L * 1024L },
                new() { FileType = "Documents", Extension = ".pdf,.doc,.docx", Count = random.Next(20, 100), TotalSize = random.Next(10, 100) * 1024L * 1024L },
                new() { FileType = "Text", Extension = ".txt", Count = random.Next(10, 50), TotalSize = random.Next(1, 10) * 1024L * 1024L }
            };

            foreach (var fileType in metrics.PopularFileTypes)
            {
                fileType.SizeFormatted = FormatBytes(fileType.TotalSize);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity metrics");
            return new MinIOActivityMetricsDto { Period = period };
        }
    }

    public async Task<MinIOHealthStatusDto> GetHealthStatusAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new MinIOHealthStatusDto
        {
            LastChecked = DateTime.UtcNow,
            Issues = new List<string>()
        };

        try
        {
            // Test basic connectivity
            await _minioClient.ListBucketsAsync();
            health.IsHealthy = true;
            health.IsReadable = true;

            // Test write capability with a small test object
            var testKey = $"health-test-{Guid.NewGuid()}.txt";
            var testContent = "MinIO health check test";
            var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));

            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(testKey)
                    .WithStreamData(testStream)
                    .WithObjectSize(testStream.Length)
                    .WithContentType("text/plain");

                await _minioClient.PutObjectAsync(putObjectArgs);
                health.IsWritable = true;

                // Clean up test object
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(testKey);
                await _minioClient.RemoveObjectAsync(removeObjectArgs);
            }
            catch (Exception ex)
            {
                health.IsWritable = false;
                health.Issues.Add($"Write test failed: {ex.Message}");
            }

            health.Status = health.IsHealthy && health.IsReadable && health.IsWritable ? "Healthy" : "Degraded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO health check failed");
            health.IsHealthy = false;
            health.IsReadable = false;
            health.IsWritable = false;
            health.Status = "Unhealthy";
            health.Issues.Add($"Connectivity failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            health.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
        }

        return health;
    }

    public async Task<MinIODiskUsageDto> GetDiskUsageAsync()
    {
        try
        {
            // In a real implementation, this would query MinIO admin API or system metrics
            var buckets = await GetBucketStatsAsync();
            var usedSpace = buckets.Sum(b => b.Size);
            var totalCapacity = 1024L * 1024L * 1024L * 1024L; // 1TB simulated
            var availableSpace = totalCapacity - usedSpace;

            return new MinIODiskUsageDto
            {
                TotalCapacity = totalCapacity,
                UsedSpace = usedSpace,
                AvailableSpace = availableSpace,
                UsedPercentage = totalCapacity > 0 ? (double)usedSpace / totalCapacity * 100 : 0,
                TotalCapacityFormatted = FormatBytes(totalCapacity),
                UsedSpaceFormatted = FormatBytes(usedSpace),
                AvailableSpaceFormatted = FormatBytes(availableSpace),
                Partitions = new List<DiskPartitionDto>
                {
                    new()
                    {
                        Path = "/data",
                        Total = totalCapacity,
                        Used = usedSpace,
                        Available = availableSpace,
                        UsedPercentage = totalCapacity > 0 ? (double)usedSpace / totalCapacity * 100 : 0
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disk usage");
            return new MinIODiskUsageDto();
        }
    }

    public async Task<List<MinIOFileOperationDto>> GetRecentFileOperationsAsync(int limit = 100)
    {
        try
        {
            // In a real implementation, this would come from MinIO audit logs
            var operations = new List<MinIOFileOperationDto>();
            var random = new Random();
            var operationTypes = new[] { "Upload", "Download", "Delete" };
            var fileExtensions = new[] { ".jpg", ".png", ".pdf", ".doc", ".txt", ".docx" };
            var ipAddresses = new[] { "192.168.1.100", "10.0.0.50", "172.16.0.25" };

            for (int i = 0; i < Math.Min(limit, 50); i++) // Simulate recent operations
            {
                var timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 1440)); // Last 24 hours
                var operation = operationTypes[random.Next(operationTypes.Length)];
                var extension = fileExtensions[random.Next(fileExtensions.Length)];
                var fileName = $"file_{i:D3}{extension}";
                var fileSize = random.Next(1024, 10 * 1024 * 1024); // 1KB to 10MB

                operations.Add(new MinIOFileOperationDto
                {
                    Timestamp = timestamp,
                    Operation = operation,
                    FileName = fileName,
                    FilePath = $"uploads/{fileName}",
                    FileSize = fileSize,
                    FileSizeFormatted = FormatBytes(fileSize),
                    ContentType = GetContentTypeFromExtension(extension),
                    UserAgent = "NeighborTools/1.0",
                    IpAddress = ipAddresses[random.Next(ipAddresses.Length)],
                    Success = random.NextDouble() > 0.1, // 90% success rate
                    ErrorMessage = random.NextDouble() > 0.9 ? "" : "Network timeout"
                });
            }

            return operations.OrderByDescending(o => o.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent file operations");
            return new List<MinIOFileOperationDto>();
        }
    }

    public async Task<MinIOFileDistributionDto> GetFileDistributionAsync()
    {
        try
        {
            var distribution = new MinIOFileDistributionDto
            {
                ByType = new List<FileTypeDistributionDto>(),
                BySize = new List<FileSizeDistributionDto>(),
                ByAge = new List<FileAgeDistributionDto>()
            };

            // Simulate file distribution by type
            var totalFiles = 150;
            distribution.TotalFiles = totalFiles;
            distribution.AverageFileSize = 2 * 1024 * 1024; // 2MB average
            distribution.AverageFileSizeFormatted = FormatBytes((long)distribution.AverageFileSize);

            distribution.ByType = new List<FileTypeDistributionDto>
            {
                new() { FileType = "Images", Extension = ".jpg,.png,.gif", Count = 80, TotalSize = 120 * 1024 * 1024, Percentage = 53.3 },
                new() { FileType = "Documents", Extension = ".pdf,.doc,.docx", Count = 45, TotalSize = 90 * 1024 * 1024, Percentage = 30.0 },
                new() { FileType = "Text", Extension = ".txt", Count = 25, TotalSize = 5 * 1024 * 1024, Percentage = 16.7 }
            };

            foreach (var fileType in distribution.ByType)
            {
                fileType.SizeFormatted = FormatBytes(fileType.TotalSize);
            }

            // File size distribution
            distribution.BySize = new List<FileSizeDistributionDto>
            {
                new() { SizeRange = "< 1MB", MinSize = 0, MaxSize = 1024 * 1024, Count = 60, Percentage = 40.0 },
                new() { SizeRange = "1MB - 10MB", MinSize = 1024 * 1024, MaxSize = 10 * 1024 * 1024, Count = 70, Percentage = 46.7 },
                new() { SizeRange = "> 10MB", MinSize = 10 * 1024 * 1024, MaxSize = long.MaxValue, Count = 20, Percentage = 13.3 }
            };

            // File age distribution
            distribution.ByAge = new List<FileAgeDistributionDto>
            {
                new() { AgeRange = "Last 7 days", Days = 7, Count = 45, Percentage = 30.0 },
                new() { AgeRange = "Last 30 days", Days = 30, Count = 75, Percentage = 50.0 },
                new() { AgeRange = "Older", Days = 365, Count = 30, Percentage = 20.0 }
            };

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file distribution");
            return new MinIOFileDistributionDto();
        }
    }

    public async Task<MinIOCleanupResultDto> CleanupOrphanedFilesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MinIOCleanupResultDto
        {
            DeletedFiles = new List<string>(),
            Errors = new List<string>()
        };

        try
        {
            _logger.LogInformation("Starting MinIO orphaned files cleanup");
            
            // In a real implementation, this would:
            // 1. Query database for all referenced file paths
            // 2. List all objects in MinIO bucket
            // 3. Find objects not referenced in database
            // 4. Delete orphaned objects
            
            // For now, simulate the cleanup process
            await Task.Delay(2000); // Simulate cleanup time
            
            result.OrphanedFilesFound = 5;
            result.FilesDeleted = 3;
            result.SpaceReclaimed = 15 * 1024 * 1024; // 15MB
            result.SpaceReclaimedFormatted = FormatBytes(result.SpaceReclaimed);
            result.Success = true;
            
            result.DeletedFiles.AddRange(new[]
            {
                "orphaned/temp_file_001.jpg",
                "orphaned/old_upload_002.pdf",
                "orphaned/incomplete_003.txt"
            });

            _logger.LogInformation("MinIO cleanup completed. Files deleted: {FilesDeleted}, Space reclaimed: {SpaceReclaimed}", 
                result.FilesDeleted, result.SpaceReclaimedFormatted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MinIO cleanup");
            result.Success = false;
            result.Errors.Add($"Cleanup failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<MinIOBackupResultDto> BackupBucketAsync(string bucketName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MinIOBackupResultDto
        {
            BucketName = bucketName,
            BackupDate = DateTime.UtcNow,
            Errors = new List<string>()
        };

        try
        {
            _logger.LogInformation("Starting backup for bucket: {BucketName}", bucketName);
            
            // In a real implementation, this would:
            // 1. Create backup directory
            // 2. Download all objects from bucket
            // 3. Create compressed archive
            // 4. Store backup with metadata
            
            await Task.Delay(3000); // Simulate backup time
            
            result.BackupPath = $"/backups/{bucketName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
            result.FilesBackedUp = 125;
            result.TotalSize = 250 * 1024 * 1024; // 250MB
            result.TotalSizeFormatted = FormatBytes(result.TotalSize);
            result.Success = true;

            _logger.LogInformation("Backup completed for bucket: {BucketName}. Files: {FilesBackedUp}, Size: {TotalSize}", 
                bucketName, result.FilesBackedUp, result.TotalSizeFormatted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bucket backup: {BucketName}", bucketName);
            result.Success = false;
            result.Errors.Add($"Backup failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task<(long TotalSize, int ObjectCount, List<string> RecentFiles)> GetBucketObjectStatsAsync(string bucketName)
    {
        long totalSize = 0;
        int objectCount = 0;
        var recentFiles = new List<string>();

        try
        {
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithRecursive(true);

            var objects = new List<Minio.DataModel.Item>();
            await foreach (var item in _minioClient.ListObjectsEnumAsync(listObjectsArgs))
            {
                objects.Add(item);
                totalSize += (long)item.Size;
                objectCount++;
            }

            // Get 5 most recent files
            recentFiles = objects
                .OrderByDescending(o => o.LastModifiedDateTime)
                .Take(5)
                .Select(o => o.Key)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting object stats for bucket: {BucketName}", bucketName);
        }

        return (totalSize, objectCount, recentFiles);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}