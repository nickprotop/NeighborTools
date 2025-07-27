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
        
        // Note: Bucket existence is checked when needed in each operation
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "")
    {
        // Default to public access for backward compatibility
        var defaultMetadata = new FileAccessMetadata
        {
            AccessLevel = "public",
            FileType = folder switch
            {
                "images" => "tool-image",
                "avatars" => "avatar",
                _ => "general"
            }
        };
        
        return await UploadFileAsync(fileStream, fileName, contentType, folder, defaultMetadata);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder, FileAccessMetadata? metadata)
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

            // Prepare upload arguments with metadata
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            // Add metadata headers if provided
            if (metadata != null)
            {
                var headers = metadata.ToHeaders();
                foreach (var header in headers)
                {
                    putObjectArgs = putObjectArgs.WithHeaders(new Dictionary<string, string> { { header.Key, header.Value } });
                }
            }

            await _minioClient.PutObjectAsync(putObjectArgs);
            
            _logger.LogInformation("File uploaded successfully to MinIO: {FileName} -> {ObjectName} with access level: {AccessLevel}", 
                fileName, objectName, metadata?.AccessLevel ?? "public");
            
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

    public async Task<FileAccessMetadata?> GetFileMetadataAsync(string storagePath)
    {
        try
        {
            await EnsureBucketExistsAsync();

            // Get object metadata
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(storagePath);

            var objectStat = await _minioClient.StatObjectAsync(statObjectArgs);
            
            // Convert metadata from object stat
            return FileAccessMetadata.FromHeaders(objectStat.MetaData);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("File not found in MinIO when getting metadata: {StoragePath}", storagePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata from MinIO: {StoragePath}", storagePath);
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
        // Return raw storage path for frontend URL construction
        return storagePath;
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