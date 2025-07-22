using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Download a file by its storage path
    /// </summary>
    /// <param name="fileName">The file name/storage path (URL encoded)</param>
    /// <returns>File stream</returns>
    [HttpGet("download/{*fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        try
        {
            _logger.LogInformation("=== FILE DOWNLOAD DEBUG START ===");
            _logger.LogInformation("Raw fileName parameter: '{FileName}'", fileName);
            
            // Decode the file name
            var decodedFileName = Uri.UnescapeDataString(fileName);
            
            _logger.LogInformation("Decoded fileName: '{DecodedFileName}'", decodedFileName);
            _logger.LogInformation("Attempting to download file: {FileName}", decodedFileName);

            // Get file stream from storage
            _logger.LogInformation("Calling _fileStorageService.DownloadFileAsync with: '{DecodedFileName}'", decodedFileName);
            var fileStream = await _fileStorageService.DownloadFileAsync(decodedFileName);
            
            _logger.LogInformation("FileStream result: {IsNull}", fileStream == null ? "NULL" : "NOT NULL");
            
            if (fileStream == null)
            {
                _logger.LogWarning("File not found: {FileName}", decodedFileName);
                _logger.LogInformation("=== FILE DOWNLOAD DEBUG END (FILE NOT FOUND) ===");
                return NotFound(new { message = "File not found" });
            }

            // Determine content type based on file extension
            var contentType = GetContentTypeFromFileName(decodedFileName);
            
            // Get original filename for download
            var originalFileName = Path.GetFileName(decodedFileName);
            
            _logger.LogInformation("Successfully serving file: {FileName}", decodedFileName);
            _logger.LogInformation("Content-Type: {ContentType}, Original filename: {OriginalFileName}", contentType, originalFileName);
            _logger.LogInformation("=== FILE DOWNLOAD DEBUG END (SUCCESS) ===");

            // Return file stream
            return File(fileStream, contentType, originalFileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
            _logger.LogInformation("=== FILE DOWNLOAD DEBUG END (EXCEPTION) ===");
            return StatusCode(500, new { message = "Internal server error while downloading file" });
        }
    }

    /// <summary>
    /// Upload a file (requires authentication)
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">Optional folder to organize files</param>
    /// <returns>File storage information</returns>
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string folder = "")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            _logger.LogInformation("Uploading file: {FileName} ({FileSize} bytes)", file.FileName, file.Length);

            using var fileStream = file.OpenReadStream();
            var storagePath = await _fileStorageService.UploadFileAsync(
                fileStream, 
                file.FileName, 
                file.ContentType, 
                folder);

            var fileUrl = await _fileStorageService.GetFileUrlAsync(storagePath);

            _logger.LogInformation("Successfully uploaded file: {FileName} -> {StoragePath}", file.FileName, storagePath);

            return Ok(new
            {
                message = "File uploaded successfully",
                storagePath,
                fileUrl,
                fileName = file.FileName,
                contentType = file.ContentType,
                size = file.Length
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload attempt: {FileName}", file?.FileName);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, new { message = "Internal server error while uploading file" });
        }
    }

    /// <summary>
    /// Delete a file (requires authentication)
    /// </summary>
    /// <param name="fileName">The file name/storage path (URL encoded)</param>
    /// <returns>Success status</returns>
    [HttpDelete("delete/{*fileName}")]
    [Authorize]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        try
        {
            // Decode the file name
            var decodedFileName = Uri.UnescapeDataString(fileName);
            
            _logger.LogInformation("Attempting to delete file: {FileName}", decodedFileName);

            var success = await _fileStorageService.DeleteFileAsync(decodedFileName);
            
            if (!success)
            {
                _logger.LogWarning("File not found for deletion: {FileName}", decodedFileName);
                return NotFound(new { message = "File not found" });
            }

            _logger.LogInformation("Successfully deleted file: {FileName}", decodedFileName);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return StatusCode(500, new { message = "Internal server error while deleting file" });
        }
    }

    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}