namespace ToolsSharing.Core.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the storage path
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "");
    
    /// <summary>
    /// Downloads a file from storage
    /// </summary>
    Task<Stream?> DownloadFileAsync(string storagePath);
    
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