namespace ToolsSharing.Core.DTOs.Admin;

public class MinIOServerStatsDto
{
    public string Version { get; set; } = "";
    public DateTime Uptime { get; set; }
    public long TotalStorage { get; set; }
    public long UsedStorage { get; set; }
    public long FreeStorage { get; set; }
    public int TotalObjects { get; set; }
    public int TotalBuckets { get; set; }
    public bool IsOnline { get; set; }
    public string Region { get; set; } = "";
}

public class MinIOBucketStatsDto
{
    public string Name { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public long Size { get; set; }
    public int ObjectCount { get; set; }
    public string SizeFormatted { get; set; } = "";
    public List<string> RecentFiles { get; set; } = new();
}

public class MinIOStorageAnalyticsDto
{
    public TimeSpan Period { get; set; }
    public List<StorageDataPointDto> StorageGrowth { get; set; } = new();
    public List<StorageDataPointDto> ObjectGrowth { get; set; } = new();
    public double GrowthRatePerDay { get; set; }
    public long PredictedSizeNextMonth { get; set; }
}

public class StorageDataPointDto
{
    public DateTime Date { get; set; }
    public long Value { get; set; }
    public string FormattedValue { get; set; } = "";
}

public class MinIOActivityMetricsDto
{
    public TimeSpan Period { get; set; }
    public int TotalUploads { get; set; }
    public int TotalDownloads { get; set; }
    public int TotalDeletes { get; set; }
    public long TotalBytesUploaded { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public List<ActivityDataPointDto> DailyActivity { get; set; } = new();
    public List<PopularFileTypeDto> PopularFileTypes { get; set; } = new();
}

public class ActivityDataPointDto
{
    public DateTime Date { get; set; }
    public int Uploads { get; set; }
    public int Downloads { get; set; }
    public int Deletes { get; set; }
}

public class PopularFileTypeDto
{
    public string FileType { get; set; } = "";
    public string Extension { get; set; } = "";
    public int Count { get; set; }
    public long TotalSize { get; set; }
    public string SizeFormatted { get; set; } = "";
}

public class MinIOHealthStatusDto
{
    public bool IsHealthy { get; set; }
    public bool IsWritable { get; set; }
    public bool IsReadable { get; set; }
    public int ResponseTimeMs { get; set; }
    public DateTime LastChecked { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Status { get; set; } = "";
}

public class MinIODiskUsageDto
{
    public long TotalCapacity { get; set; }
    public long UsedSpace { get; set; }
    public long AvailableSpace { get; set; }
    public double UsedPercentage { get; set; }
    public string TotalCapacityFormatted { get; set; } = "";
    public string UsedSpaceFormatted { get; set; } = "";
    public string AvailableSpaceFormatted { get; set; } = "";
    public List<DiskPartitionDto> Partitions { get; set; } = new();
}

public class DiskPartitionDto
{
    public string Path { get; set; } = "";
    public long Total { get; set; }
    public long Used { get; set; }
    public long Available { get; set; }
    public double UsedPercentage { get; set; }
}

public class MinIOFileOperationDto
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = ""; // Upload, Download, Delete
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string FileSizeFormatted { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public class MinIOFileDistributionDto
{
    public List<FileTypeDistributionDto> ByType { get; set; } = new();
    public List<FileSizeDistributionDto> BySize { get; set; } = new();
    public List<FileAgeDistributionDto> ByAge { get; set; } = new();
    public double AverageFileSize { get; set; }
    public string AverageFileSizeFormatted { get; set; } = "";
    public int TotalFiles { get; set; }
}

public class FileTypeDistributionDto
{
    public string FileType { get; set; } = "";
    public string Extension { get; set; } = "";
    public int Count { get; set; }
    public long TotalSize { get; set; }
    public double Percentage { get; set; }
    public string SizeFormatted { get; set; } = "";
}

public class FileSizeDistributionDto
{
    public string SizeRange { get; set; } = "";
    public long MinSize { get; set; }
    public long MaxSize { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class FileAgeDistributionDto
{
    public string AgeRange { get; set; } = "";
    public int Days { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class MinIOCleanupResultDto
{
    public int OrphanedFilesFound { get; set; }
    public int FilesDeleted { get; set; }
    public long SpaceReclaimed { get; set; }
    public string SpaceReclaimedFormatted { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public List<string> DeletedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool Success { get; set; }
}

public class MinIOBackupResultDto
{
    public string BucketName { get; set; } = "";
    public string BackupPath { get; set; } = "";
    public int FilesBackedUp { get; set; }
    public long TotalSize { get; set; }
    public string TotalSizeFormatted { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public DateTime BackupDate { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class MinIODashboardDto
{
    public MinIOServerStatsDto ServerStats { get; set; } = new();
    public List<MinIOBucketStatsDto> BucketStats { get; set; } = new();
    public MinIOHealthStatusDto HealthStatus { get; set; } = new();
    public MinIODiskUsageDto DiskUsage { get; set; } = new();
    public List<MinIOFileOperationDto> RecentOperations { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}