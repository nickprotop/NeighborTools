using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.Services;

public class MinIOFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIOFileStorageService> _logger;
    private readonly FileValidationOptions _validationOptions;
    private readonly string _bucketName;

    public MinIOFileStorageService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinIOFileStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
        _validationOptions = new FileValidationOptions();
        _bucketName = configuration["MinIO:BucketName"] ?? "toolssharing-files";
        
        // Ensure bucket exists
        _ = Task.Run(async () => await EnsureBucketExistsAsync());
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "")
    {
        if (!IsFileValid(fileName, contentType, fileStream.Length))
        {
            throw new ArgumentException("Invalid file type or size");
        }

        try
        {
            await EnsureBucketExistsAsync();

            // Create unique filename to prevent conflicts
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            
            // Create object name (path in MinIO)
            var objectName = string.IsNullOrEmpty(folder) 
                ? uniqueFileName 
                : $"{folder.Trim('/')}/{uniqueFileName}";

            // Upload file to MinIO
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            
            _logger.LogInformation("File uploaded successfully to MinIO: {FileName} -> {ObjectName}", fileName, objectName);
            
            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to MinIO: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream?> DownloadFileAsync(string storagePath)
    {
        try
        {
            await EnsureBucketExistsAsync();

            // Check if object exists
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(storagePath);

            await _minioClient.StatObjectAsync(statObjectArgs);

            // Get object from MinIO
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(storagePath)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("File not found in MinIO: {StoragePath}", storagePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from MinIO: {StoragePath}", storagePath);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string storagePath)
    {
        try
        {
            await EnsureBucketExistsAsync();

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(storagePath);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            
            _logger.LogInformation("File deleted from MinIO: {StoragePath}", storagePath);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("File not found for deletion in MinIO: {StoragePath}", storagePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from MinIO: {StoragePath}", storagePath);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiry = null)
    {
        // Return API endpoint URL instead of direct MinIO URL for security
        // The API will handle authentication and stream the file from MinIO
        return $"/api/files/download/{Uri.EscapeDataString(storagePath)}";
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

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            
            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("Created MinIO bucket: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring MinIO bucket exists: {BucketName}", _bucketName);
            throw;
        }
    }
}