namespace ToolsSharing.Core.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the storage path
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "");
    
    /// <summary>
    /// Uploads a file with access control metadata and returns the storage path
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder, FileAccessMetadata? metadata);
    
    /// <summary>
    /// Downloads a file from storage
    /// </summary>
    Task<Stream?> DownloadFileAsync(string storagePath);
    
    /// <summary>
    /// Gets file access metadata
    /// </summary>
    Task<FileAccessMetadata?> GetFileMetadataAsync(string storagePath);
    
    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(string storagePath);
    
    /// <summary>
    /// Gets a secure URL for file access (useful for cloud storage)
    /// </summary>
    Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiry = null);
    
    /// <summary>
    /// Validates file type and size
    /// </summary>
    bool IsFileValid(string fileName, string contentType, long fileSize);
}

/// <summary>
/// File access control metadata for MinIO objects
/// </summary>
public class FileAccessMetadata
{
    /// <summary>
    /// Access level: "public", "private", "restricted"
    /// </summary>
    public string AccessLevel { get; set; } = "public";
    
    /// <summary>
    /// User ID who owns/uploaded the file
    /// </summary>
    public string? OwnerId { get; set; }
    
    /// <summary>
    /// Related dispute ID for dispute evidence files
    /// </summary>
    public string? DisputeId { get; set; }
    
    /// <summary>
    /// Related rental ID for rental-related files
    /// </summary>
    public string? RentalId { get; set; }
    
    /// <summary>
    /// Comma-separated list of user IDs who can access this file
    /// </summary>
    public string? AllowedUsers { get; set; }
    
    /// <summary>
    /// File type for categorization: "dispute-evidence", "tool-image", "avatar", "general"
    /// </summary>
    public string FileType { get; set; } = "general";
    
    /// <summary>
    /// Convert metadata to MinIO headers dictionary
    /// </summary>
    public Dictionary<string, string> ToHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["x-amz-meta-access-level"] = AccessLevel,
            ["x-amz-meta-file-type"] = FileType
        };
        
        if (!string.IsNullOrEmpty(OwnerId))
            headers["x-amz-meta-owner-id"] = OwnerId;
            
        if (!string.IsNullOrEmpty(DisputeId))
            headers["x-amz-meta-dispute-id"] = DisputeId;
            
        if (!string.IsNullOrEmpty(RentalId))
            headers["x-amz-meta-rental-id"] = RentalId;
            
        if (!string.IsNullOrEmpty(AllowedUsers))
            headers["x-amz-meta-allowed-users"] = AllowedUsers;
            
        return headers;
    }
    
    /// <summary>
    /// Create metadata from MinIO headers dictionary
    /// </summary>
    public static FileAccessMetadata? FromHeaders(Dictionary<string, string>? headers)
    {
        if (headers == null) return null;
        
        var metadata = new FileAccessMetadata();
        
        if (headers.TryGetValue("x-amz-meta-access-level", out var accessLevel))
            metadata.AccessLevel = accessLevel;
            
        if (headers.TryGetValue("x-amz-meta-file-type", out var fileType))
            metadata.FileType = fileType;
            
        if (headers.TryGetValue("x-amz-meta-owner-id", out var ownerId))
            metadata.OwnerId = ownerId;
            
        if (headers.TryGetValue("x-amz-meta-dispute-id", out var disputeId))
            metadata.DisputeId = disputeId;
            
        if (headers.TryGetValue("x-amz-meta-rental-id", out var rentalId))
            metadata.RentalId = rentalId;
            
        if (headers.TryGetValue("x-amz-meta-allowed-users", out var allowedUsers))
            metadata.AllowedUsers = allowedUsers;
            
        return metadata;
    }
    
    /// <summary>
    /// Check if a user ID is allowed to access this file
    /// </summary>
    public bool IsUserAllowed(string userId, bool isAdmin = false)
    {
        // Admins can access everything
        if (isAdmin) return true;
        
        // Public files are accessible to everyone
        if (AccessLevel == "public") return true;
        
        // Owner can always access their files
        if (OwnerId == userId) return true;
        
        // Check allowed users list
        if (!string.IsNullOrEmpty(AllowedUsers))
        {
            var allowedUsersList = AllowedUsers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return allowedUsersList.Contains(userId);
        }
        
        // Private files without explicit permissions are denied
        return false;
    }
}

public class FileValidationOptions
{
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff"
    };
    
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/bmp",
        "image/tiff"
    };
    
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
}