using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly FileValidationOptions _validationOptions;
    private readonly string _basePath;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _validationOptions = new FileValidationOptions();
        
        // Get storage path from configuration, default to wwwroot/uploads
        _basePath = _configuration["FileStorage:BasePath"] ?? Path.Combine("wwwroot", "uploads");
        
        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "")
    {
        if (!IsFileValid(fileName, contentType, fileStream.Length))
        {
            throw new ArgumentException("Invalid file type or size");
        }

        try
        {
            // Create unique filename to prevent conflicts
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            
            // Create folder path
            var folderPath = string.IsNullOrEmpty(folder) ? _basePath : Path.Combine(_basePath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Full file path
            var filePath = Path.Combine(folderPath, uniqueFileName);
            
            // Save file
            using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamOutput);
            
            // Return relative path for storage
            var relativePath = string.IsNullOrEmpty(folder) ? uniqueFileName : Path.Combine(folder, uniqueFileName);
            
            _logger.LogInformation("File uploaded successfully: {FileName} -> {StoragePath}", fileName, relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream?> DownloadFileAsync(string storagePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {StoragePath}", storagePath);
                return null;
            }
            
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return await Task.FromResult(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {StoragePath}", storagePath);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string storagePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {StoragePath}", storagePath);
                return await Task.FromResult(true);
            }
            
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {StoragePath}", storagePath);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiry = null)
    {
        // For local storage, return a relative URL that can be served by the web server
        var baseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";
        var url = $"{baseUrl.TrimEnd('/')}/{storagePath.Replace('\\', '/')}";
        
        return await Task.FromResult(url);
    }

    public bool IsFileValid(string fileName, string contentType, long fileSize)
    {
        // Check file size
        if (fileSize > _validationOptions.MaxFileSize)
        {
            _logger.LogWarning("File too large: {FileName} ({FileSize} bytes)", fileName, fileSize);
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_validationOptions.AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid file extension: {FileName} ({Extension})", fileName, extension);
            return false;
        }

        // Check content type
        if (!_validationOptions.AllowedContentTypes.Contains(contentType?.ToLowerInvariant() ?? ""))
        {
            _logger.LogWarning("Invalid content type: {FileName} ({ContentType})", fileName, contentType);
            return false;
        }

        return true;
    }
}