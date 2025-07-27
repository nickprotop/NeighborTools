using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;
    private readonly ApplicationDbContext _context;

    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger,
        ApplicationDbContext context)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Download a file by its storage path (public files accessible to all, private files require authorization)
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
            
            // Validate filename parameter
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Empty or null filename provided");
                return BadRequest(new { message = "Filename is required" });
            }
            
            // Decode the file name and normalize path
            var decodedFileName = Uri.UnescapeDataString(fileName);
            
            // Normalize path separators and remove double slashes
            decodedFileName = decodedFileName.Replace("\\", "/");
            while (decodedFileName.Contains("//"))
            {
                decodedFileName = decodedFileName.Replace("//", "/");
            }
            
            // Remove leading slash if present
            if (decodedFileName.StartsWith("/"))
            {
                decodedFileName = decodedFileName.Substring(1);
            }
            
            _logger.LogInformation("Normalized fileName: '{DecodedFileName}'", decodedFileName);
            
            // Validate normalized filename
            if (string.IsNullOrWhiteSpace(decodedFileName))
            {
                _logger.LogWarning("Filename became empty after normalization");
                return BadRequest(new { message = "Invalid filename" });
            }
            
            _logger.LogInformation("Attempting to download file: {FileName}", decodedFileName);

            // Check authorization for private folders
            if (!await IsFileAccessAuthorizedAsync(decodedFileName))
            {
                _logger.LogWarning("Unauthorized access attempt to file: {FileName}", decodedFileName);
                return StatusCode(403, new { message = "Access denied to this file" });
            }

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
    /// Upload a file with optional metadata (requires authentication)
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">Optional folder to organize files</param>
    /// <param name="accessLevel">Access level: 'public', 'private', or 'restricted'. Defaults to 'private'</param>
    /// <param name="fileType">File type category for organization. Defaults to 'general'</param>
    /// <param name="allowedUsers">Comma-separated list of user IDs who can access this file (for restricted access)</param>
    /// <param name="relatedId">Related entity ID (e.g., rental ID, dispute ID) for context</param>
    /// <returns>File storage information</returns>
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadFile(
        IFormFile file, 
        [FromQuery] string folder = "",
        [FromQuery] string? accessLevel = null,
        [FromQuery] string? fileType = null,
        [FromQuery] string? allowedUsers = null,
        [FromQuery] string? relatedId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Get current user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            _logger.LogInformation("Uploading file: {FileName} ({FileSize} bytes) by user {UserId}", 
                file.FileName, file.Length, userId);

            // Create metadata with secure defaults
            var metadata = new FileAccessMetadata
            {
                // Default to private access if not specified (security first)
                AccessLevel = !string.IsNullOrEmpty(accessLevel) ? accessLevel.ToLowerInvariant() : "private",
                FileType = !string.IsNullOrEmpty(fileType) ? fileType : "general",
                OwnerId = userId,
                AllowedUsers = allowedUsers
            };

            // Validate access level
            var validAccessLevels = new[] { "public", "private", "restricted" };
            if (!validAccessLevels.Contains(metadata.AccessLevel))
            {
                return BadRequest(new { message = "Invalid access level. Must be 'public', 'private', or 'restricted'" });
            }

            // Set related entity IDs based on context
            if (!string.IsNullOrEmpty(relatedId))
            {
                // Try to determine the type of related ID based on file type or folder
                if (metadata.FileType == "dispute-evidence" || folder.StartsWith("disputes/"))
                {
                    metadata.DisputeId = relatedId;
                }
                else if (metadata.FileType == "rental-document" || folder.StartsWith("rentals/"))
                {
                    metadata.RentalId = relatedId;
                }
                // Add more context mappings as needed
            }

            _logger.LogInformation("File metadata: AccessLevel={AccessLevel}, FileType={FileType}, OwnerId={OwnerId}", 
                metadata.AccessLevel, metadata.FileType, metadata.OwnerId);

            using var fileStream = file.OpenReadStream();
            var storagePath = await _fileStorageService.UploadFileAsync(
                fileStream, 
                file.FileName, 
                file.ContentType, 
                folder,
                metadata);

            var fileUrl = await _fileStorageService.GetFileUrlAsync(storagePath);

            _logger.LogInformation("Successfully uploaded file: {FileName} -> {StoragePath}", file.FileName, storagePath);

            return Ok(new
            {
                message = "File uploaded successfully",
                storagePath,
                fileUrl,
                fileName = file.FileName,
                contentType = file.ContentType,
                size = file.Length,
                metadata = new
                {
                    accessLevel = metadata.AccessLevel,
                    fileType = metadata.FileType,
                    ownerId = metadata.OwnerId
                }
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

    /// <summary>
    /// Check if the current user is authorized to access the specified file using metadata-based authorization
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if access is authorized, false otherwise</returns>
    private async Task<bool> IsFileAccessAuthorizedAsync(string filePath)
    {
        try
        {
            // Get file metadata from MinIO
            var metadata = await _fileStorageService.GetFileMetadataAsync(filePath);
            
            // If no metadata exists, fall back to path-based authorization for backward compatibility
            if (metadata == null)
            {
                return await IsPathBasedAuthorizationAsync(filePath);
            }

            // Get current user info
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var isAdmin = User.IsInRole("Admin");

            // Use metadata-based authorization
            if (!isAuthenticated && metadata.AccessLevel != "public")
            {
                return false;
            }

            return metadata.IsUserAllowed(userId ?? "", isAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file authorization for: {FilePath}", filePath);
            // In case of error, deny access to be safe
            return false;
        }
    }

    /// <summary>
    /// Fallback authorization method for files without metadata (backward compatibility)
    /// </summary>
    private async Task<bool> IsPathBasedAuthorizationAsync(string filePath)
    {
        // Public folders - accessible to everyone
        if (filePath.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Private folders require authentication
        if (filePath.StartsWith("disputes/", StringComparison.OrdinalIgnoreCase))
        {
            // Must be authenticated to access dispute files
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // Check if user is admin
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            // Extract dispute ID from path (disputes/{disputeId}/filename)
            var pathParts = filePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length >= 2 && Guid.TryParse(pathParts[1], out var disputeId))
            {
                // Check if user is involved in the dispute
                var dispute = await _context.Disputes
                    .Include(d => d.Rental)
                        .ThenInclude(r => r.Tool)
                    .FirstOrDefaultAsync(d => d.Id == disputeId);

                if (dispute != null)
                {
                    // User can access if they are the dispute creator, renter, or tool owner
                    return dispute.InitiatedBy == userId ||
                           dispute.Rental.RenterId == userId ||
                           dispute.Rental.Tool.OwnerId == userId;
                }
            }

            return false;
        }

        // For any other private folders, require authentication
        return User.Identity?.IsAuthenticated ?? false;
    }
}